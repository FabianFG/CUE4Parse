using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.Actions;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwiseExtractedSound
{
    public required string OutputPath { get; init; }
    public required string Extension { get; init; }
    public required FDeferredByteData? Data { get; init; }

    public byte[] GetData() => Data?.GetData() ?? [];
    public override string ToString() => OutputPath + "." + Extension.ToLowerInvariant();
}

public partial class WwiseProvider
{
    private readonly AbstractVfsFileProvider _provider;
    private readonly string _gameDirectory;
    private string? _baseWwiseAudioPath;

    private static readonly HashSet<string> _validWwiseExtensions = new(StringComparer.OrdinalIgnoreCase) { "bnk", "pck", "wem" };
    private readonly Dictionary<uint, Hierarchy> _wwiseHierarchyTables = [];
    private readonly Dictionary<uint, List<Hierarchy>> _wwiseHierarchyDuplicates = [];
    private readonly Dictionary<string, FDeferredByteData> _wwiseEncodedMedia = [];
    private readonly HashSet<uint> _wwiseLoadedSoundBanks = [];
    private readonly Dictionary<uint, WwiseReader> _multiReferenceLibraryCache = [];
    private bool _completedWwiseFullBnkInit = false;
    private bool _loadedMultiRefLibrary = false;
    private long _totalLoadedWwiseSize = 0L;
    private long _totalWwiseBanksSize = 0L;

    private readonly Dictionary<uint, FGameFileDeferredByteData> _looseWemFilesLookup = [];

    public WwiseProvider(AbstractVfsFileProvider provider, string gameDirectory)
    {
        _provider = provider;
        _gameDirectory = gameDirectory;

        LoadMultiReferenceLibrary();

        BulkInitializeWwise();
        if (!_completedWwiseFullBnkInit)
            throw new InvalidOperationException("Failed to initialize Wwise soundbanks. Ensure that the provider has files to work with.");
    }

    private readonly HashSet<(uint Id, Hierarchy Hierarchy)> _visitedHierarchies = []; // To speed things up
    private readonly HashSet<uint> _visitedWemIds = []; // To prevent duplicates

    // Please don't change this, when extracting directly from .bnk we shouldn't loop through wwise hierarchy
    // because that doesn't guarantee us to extract the audio from this given soundbank
    public List<WwiseExtractedSound> ExtractBankSounds(WwiseReader wwiseReader)
    {
        CacheWwiseFile(wwiseReader);
        var ownerDirectory = wwiseReader.Path.SubstringBeforeLast('.');

        if (wwiseReader.WwiseEncodedMedias == null)
            return [];

        var results = new List<WwiseExtractedSound>(wwiseReader.WwiseEncodedMedias.Count);
        foreach (var media in wwiseReader.WwiseEncodedMedias)
        {
            var data = media.Value;
            if (uint.TryParse(media.Key, out var id) && _looseWemFilesLookup.TryGetValue(id, out var looseWemFile) && looseWemFile.IsValid)
            {
                data = looseWemFile;
            }

            results.Add(new WwiseExtractedSound
            {
                OutputPath = Path.Combine(ownerDirectory, media.Key),
                Extension = "wem",
                Data = data,
            });
        }

        return results;
    }

    public List<WwiseExtractedSound> ExtractBankSounds(UAkAudioBank audioBank)
    {
        DetermineBaseWwiseAudioPath();

        var soundBankCookedData = audioBank.SoundBankCookedData;

        if (soundBankCookedData is null)
        {
            var soundBankId = audioBank.GetOrDefault<uint>("ShortID");

            if (soundBankId is 0)
                return [];

            var soundBank = LoadSoundBankById(soundBankId, returnBank: true);

            if (soundBank is null)
                return [];

            return ExtractBankSounds(soundBank);
        }
        else
        {
            var results = new List<WwiseExtractedSound>();
            foreach (var eventName in soundBankCookedData.IncludedEventNames)
            {
                if (eventName.IsNone)
                    continue;

                var audioEventId = WwiseFnv.GetHash(eventName.Text);
                LoopThroughEvent(audioEventId, results, _baseWwiseAudioPath, eventName.Text);
            }

            return results;
        }
    }

    public List<WwiseExtractedSound> ExtractAudioEventSounds(UAkAudioEvent audioEvent)
    {
        DetermineBaseWwiseAudioPath(audioEvent);

        _visitedHierarchies.Clear();
        _visitedWemIds.Clear();

        var results = new List<WwiseExtractedSound>();

        var wwiseData = audioEvent.EventCookedData;
        if (wwiseData == null)
        {
            var requiredBankProp = audioEvent.Properties.FirstOrDefault(p => p.Name.Text == "RequiredBank");
            var shortIdProp = audioEvent.Properties.FirstOrDefault(p => p.Name.Text == "ShortID");

            if (shortIdProp?.Tag?.GenericValue is not uint audioEventId || requiredBankProp?.Tag?.ToString() == null)
            {
                audioEventId = WwiseFnv.GetHash(audioEvent.Name);
            }

            string? soundBankId = null;
            if (requiredBankProp?.Tag is ObjectProperty { Value: not null } objProp)
            {
                if (objProp.Value.TryLoad(out var audioBank))
                {
                    soundBankId = audioBank?.Properties?.FirstOrDefault(p => p.Name.Text == "ShortID")?.Tag?.GenericValue?.ToString();
                }
            }

            if (!string.IsNullOrEmpty(soundBankId))
                LoadSoundBankById(uint.Parse(soundBankId));

            LoopThroughEvent(audioEventId, results, _baseWwiseAudioPath, audioEvent.Name);

            return results;
        }

        // cache all banks first
        foreach (var (languageData, eventData) in wwiseData.Value.EventLanguageMap)
        {
            if (!eventData.HasValue)
                continue;

            foreach (var soundBank in eventData.Value.SoundBanks)
            {
                CacheSoundBankCookedData(soundBank);
            }

            foreach (var leaf in eventData.Value.SwitchContainerLeaves)
            {
                foreach (var soundBank in leaf.SoundBanks)
                {
                    CacheSoundBankCookedData(soundBank);
                }
            }
        }

        foreach (var (languageData, eventData) in wwiseData.Value.EventLanguageMap)
        {
            if (!eventData.HasValue)
                continue;

            foreach (var soundBank in eventData.Value.SoundBanks)
            {
                ProcessSoundBankCookedData(soundBank, eventData, results);
            }

            foreach (var media in eventData.Value.Media)
            {
                ProcessMediaCookedData(media, languageData, results);
            }

            foreach (var leaf in eventData.Value.SwitchContainerLeaves)
            {
                foreach (var soundBank in leaf.SoundBanks)
                {
                    ProcessSoundBankCookedData(soundBank, eventData, results);
                }

                foreach (var media in leaf.Media)
                {
                    ProcessMediaCookedData(media, languageData, results);
                }
            }
        }

        return results;
    }

    private void ProcessMediaCookedData(FWwiseMediaCookedData media, FWwiseLanguageCookedData languageData, List<WwiseExtractedSound> results)
    {
        DetermineBaseWwiseAudioPath();

        var mediaPathName = ResolveWwisePath(media.MediaPathName.Text, media.PackagedFile, media.MediaPathName.IsNone);
        var mediaRelativePath = Path.Combine(_baseWwiseAudioPath, mediaPathName);

        FDeferredByteData? data = null;
        if (media.PackagedFile?.BulkData?.WemFile is { IsValid: true }  mediaData)
        {
            data = mediaData;
        }
        else if (_provider.TryGetGameFile(mediaRelativePath, out var providerData))
        {
            data = new FGameFileDeferredByteData(providerData);
        }
        else if (_multiReferenceLibraryCache.TryGetValue(media.PackagedFile?.Hash ?? 0, out var multiRefReader))
        {
            data = multiRefReader?.WemFile;
        }

        var mediaDebugName = !string.IsNullOrEmpty(media.DebugName.Text)
            ? media.DebugName.Text.SubstringBeforeLast('.')
            : Path.GetFileNameWithoutExtension(mediaRelativePath);

        var namedPath = Path.Combine(
            _baseWwiseAudioPath,
            $"{mediaDebugName} ({languageData.LanguageName.Text})"
        );

        results.Add(new WwiseExtractedSound
        {
            OutputPath = namedPath.Replace('\\', '/'),
            Extension = media.MediaPathName.IsNone ? "wem" : Path.GetExtension(mediaRelativePath).TrimStart('.'),
            Data = data,
        });
    }

    private void CacheSoundBankCookedData(FWwiseSoundBankCookedData soundBank)
    {
        var bulkPackagedSoundBank = soundBank.PackagedFile?.BulkData;
        if (bulkPackagedSoundBank is not null && !_wwiseLoadedSoundBanks.Contains(bulkPackagedSoundBank.Header.SoundBankId))
        {
            CacheWwiseFile(bulkPackagedSoundBank);
            _wwiseLoadedSoundBanks.Add(bulkPackagedSoundBank.Header.SoundBankId);
        }
    }

    private void ProcessSoundBankCookedData(FWwiseSoundBankCookedData soundBank, FWwiseEventCookedData? eventData, List<WwiseExtractedSound> results)
    {
        DetermineBaseWwiseAudioPath();

        var soundBankName = ResolveWwisePath(soundBank.SoundBankPathName.Text, soundBank.PackagedFile, soundBank.SoundBankPathName.IsNone);
        var soundBankPath = Path.Combine(_baseWwiseAudioPath, soundBankName);

        LoopThroughEvent(eventData!.Value.EventId, results, soundBankPath.SubstringBeforeLast('.'), eventData.Value.DebugName.Text);
    }

    private WwiseReader? LoadSoundBankById(uint soundBankId, bool returnBank = false)
    {
        if (!returnBank && _wwiseLoadedSoundBanks.Contains(soundBankId))
            return null;

        DetermineBaseWwiseAudioPath();

        var validExtensions = _validWwiseExtensions.Except(["wem"], StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in _provider.Files)
        {
            if (!file.Key.Contains(_baseWwiseAudioPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                continue;
            if (!validExtensions.Contains(Path.GetExtension(file.Key).TrimStart('.')))
                continue;

            try
            {
                using var reader = file.Value.CreateReader();
                var id = WwiseReader.TryReadSoundBankId(reader);
                if (id != soundBankId)
                    continue;

                reader.Position = 0;
                var soundBank = new WwiseReader(reader, new WwiseGameFileSource(file.Value));
                CacheWwiseFile(soundBank);
                _wwiseLoadedSoundBanks.Add(soundBankId);
                return soundBank;
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to read soundbank file '{file.Key}'");
            }
        }

        Log.Warning("Soundbank with ID {ID} wasn't found", soundBankId);

        return null;
    }

    private void LoopThroughEvent(uint eventId, List<WwiseExtractedSound> results, string ownerDirectory, string? debugName = null)
    {
        List<CAkActionSetSwitch> _switchStates = [];
        TraverseAndSave(eventId);

        void TraverseAndSave(uint id)
        {
            foreach (var hierarchy in GetHierarchiesById(id))
            {
                if (!_visitedHierarchies.Add((id, hierarchy)))
                    continue;

                switch (hierarchy.Data)
                {
                    case HierarchySoundSfxVoice soundSfx:
                        if (soundSfx.Source is { Plugin.Type: EAkPluginType.Codec })
                            SaveWemSound(soundSfx.Source.SourceId);
                        else
                            TraverseAndSave(soundSfx.Source.SourceId);
                        break;
                    case HierarchyMusicRandomSequenceContainer musicRandomSequenceContainer:
                        foreach (var childId in musicRandomSequenceContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchyMusicSwitchContainer musicSwitchContainer:
                        foreach (var childId in musicSwitchContainer.ChildIds)
                            TraverseAndSave(childId);
                        foreach (var node in musicSwitchContainer.DecisionTree.Nodes)
                            foreach (var nodeChild in node.Children)
                                TraverseDecisionTreeNode(nodeChild);

                        void TraverseDecisionTreeNode(AkDecisionTreeNode node)
                        {
                            TraverseAndSave(node.AudioNodeId);
                            foreach (var nodeChildTraverse in node.Children)
                            {
                                TraverseDecisionTreeNode(nodeChildTraverse);
                            }
                        }
                        break;
                    case HierarchyMusicTrack musicTrack:
                        foreach (var playlist in musicTrack.Playlist)
                            SaveWemSound(playlist.SourceId);
                        break;
                    case HierarchyMusicSegment musicSegment:
                        foreach (var childId in musicSegment.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchyRandomSequenceContainer randomContainer:
                        foreach (var childId in randomContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchySwitchContainer switchContainer:
                        var index = _switchStates.FindIndex(x => x.SwitchGroupId == switchContainer.GroupId);
                        if (index != -1)
                        {
                            TraverseSwitchContainer(switchContainer, _switchStates[index].SwitchStateId);
                        }
                        else if (switchContainer.DefaultSwitch == 0 || _switchStates.Count == 0)
                        {
                            foreach (var childId in switchContainer.ChildIds)
                                TraverseAndSave(childId);
                        }
                        else
                        {
                            TraverseSwitchContainer(switchContainer, switchContainer.DefaultSwitch);
                        }

                        void TraverseSwitchContainer(HierarchySwitchContainer switchContainer, uint stateId)
                        {
                            foreach (var state in switchContainer.SwitchPackages.Where(x => x.SwitchId == stateId && x.NodeIds is not null))
                            {
                                foreach (var node in state.NodeIds)
                                    TraverseAndSave(node);
                            }
                        }

                        break;
                    case HierarchyLayerContainer layerContainer:
                        foreach (var childId in layerContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    // Skip mixers cause it resolves too many sounds from other events
                    //case HierarchyActorMixer mixerContainer:
                    //    foreach (var childId in mixerContainer.ChildIds)
                    //        TraverseAndSave(childId);
                    //    break;
                    case HierarchyFxCustom fxCustom:
                        foreach (var childId in fxCustom.MediaList)
                            SaveWemSound(childId.SourceId);
                        break;
                    case HierarchyEvent eventContainer:
                        var saved = _switchStates.Count;
                        foreach (var actionId in eventContainer.EventActionIds)
                        {
                            if (!_wwiseHierarchyTables.TryGetValue(actionId, out var actionHierarchy) || actionHierarchy.Data is not HierarchyEventAction eventAction)
                                continue;

                            if (eventAction.EventActionType is EAkActionType.SetSwitch && eventAction.ActionData is CAkActionSetSwitch setSwitch)
                            {
                                _switchStates.Add(setSwitch);
                            }
                            else
                            {
                                TraverseAndSave(eventAction.ReferencedId);
                            }
                        }

                        _switchStates.RemoveRange(saved, _switchStates.Count - saved);
                        break;

                    default:
                        if (hierarchy.Type is EAKBKHircType.AudioBus or EAKBKHircType.ActorMixer) // Not needed for resolving audio
                            break;

                        Log.Warning("Unhandled hierarchy type {0}, while traversing through Event {1}", hierarchy.Type, eventId);
                        break;
                }
            }
        }

        void SaveWemSound(uint wemId)
        {
            if (!_visitedWemIds.Add(wemId))
                return;

            var fileName = wemId.ToString();
            if (_looseWemFilesLookup.TryGetValue(wemId, out var wemGameFile) | _wwiseEncodedMedia.TryGetValue(fileName, out var wemData))
            {
                if (!string.IsNullOrEmpty(debugName))
                    fileName = $"{debugName} ({fileName})";

                var outputPath = Path.Combine(ownerDirectory, fileName);
                if (outputPath.StartsWith('/'))
                    outputPath = outputPath[1..];

                var data = wemGameFile is { IsValid: true } ? wemGameFile : wemData;

                results.Add(new WwiseExtractedSound
                {
                    OutputPath = outputPath.Replace('\\', '/'),
                    Extension = "wem",
                    Data = data,
                });
            }
        }
    }

    [MemberNotNull(nameof(_baseWwiseAudioPath))]
    private void DetermineBaseWwiseAudioPath(UAkAudioEvent? audioEvent = null)
    {
        if (!string.IsNullOrEmpty(_baseWwiseAudioPath))
            return;

        _baseWwiseAudioPath = Path.Combine(_provider.ProjectName, "Content", "WwiseAudio"); // Most common directory

        var wwiseData = audioEvent?.EventCookedData;
        if (wwiseData == null)
            return;

        var eventData = wwiseData.Value.EventLanguageMap
            .Select(kv => kv.Value)
            .FirstOrDefault(v => v.HasValue);

        var files = _provider.Files.Values.ToList();
        var soundBankName = eventData?.SoundBanks.FirstOrDefault()?.SoundBankPathName.ToString() ?? string.Empty;
        var mediaPathName = eventData?.Media.FirstOrDefault().MediaPathName.Text ?? string.Empty;

        var targets = new[]
        {
            eventData?.SoundBanks.FirstOrDefault()?.SoundBankPathName.ToString(),
            eventData?.Media.FirstOrDefault().MediaPathName.Text
        };

        foreach (var target in targets)
        {
            if (string.IsNullOrEmpty(target) || target == "None")
                continue;

            var matchingFile = files.FirstOrDefault(f => f.Path.Contains(target) &&
                                                         !f.Path.StartsWith("Engine/", StringComparison.OrdinalIgnoreCase));
            if (matchingFile != null)
            {
                var matchingDirectory = matchingFile.Path.SubstringBefore(target);
                _baseWwiseAudioPath = matchingDirectory.Replace('/', Path.DirectorySeparatorChar);
                return;
            }
        }
    }

    private int LoadExternalWwiseFiles()
    {
        var searchDirectory = _gameDirectory;
        var dir = new DirectoryInfo(searchDirectory);
        if (!dir.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (Directory.GetParent(searchDirectory) is { } parentInfo)
            searchDirectory = parentInfo.FullName;

        var wwiseDir = Directory.EnumerateDirectories(searchDirectory, "WwiseAudio", SearchOption.AllDirectories)
                           .FirstOrDefault(Directory.Exists);

        if (wwiseDir is null)
        {
            Log.Warning($"Wwise directory not found under '{wwiseDir}'");
            return 0;
        }

        var wemFiles = Directory.GetFiles(wwiseDir, "*.wem", SearchOption.AllDirectories);
        var wwiseBankFiles = Directory.EnumerateFiles(wwiseDir, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".bnk") || s.EndsWith(".pck"));

        foreach (var wem in wemFiles)
        {
            var idString = Path.GetFileNameWithoutExtension(wem);
            if (uint.TryParse(idString, out var wemId))
            {
                var gameFile = new OsGameFile(new FileInfo(wem), _provider.Versions);
                _looseWemFilesLookup[wemId] = new FGameFileDeferredByteData(gameFile);
            }
        }

        var loadedBanks = 0;
        foreach (var bnk in wwiseBankFiles)
        {
            var gameFile = new OsGameFile(new FileInfo(bnk), _provider.Versions);
            if (TryLoadAndCacheWwiseFile(gameFile))
                loadedBanks++;
        }
        return loadedBanks;
    }

    private void BulkInitializeWwise()
    {
        if (_completedWwiseFullBnkInit)
            return;

        int totalLoadedBanks = _multiReferenceLibraryCache.Count;
        totalLoadedBanks += LoadExternalWwiseFiles();

        var wwiseFiles = _provider.Files.Values
            .Where(file => _validWwiseExtensions.Contains(file.Extension))
            // We need to prioritize .pck over .bnk (if there's such pair, .bnk might contain only partial audio buffer, full one is stored in .pck)
            .OrderByDescending(file => file.Extension.Equals("pck", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (wwiseFiles.Count == 0)
        {
            var initAsset = _provider.Files.Values.Any(file => file.Extension.Equals("uasset", StringComparison.OrdinalIgnoreCase) &&
                                                               (file.Path.Contains("Init", StringComparison.OrdinalIgnoreCase) ||
                                                                file.Path.Contains("InitBank", StringComparison.OrdinalIgnoreCase)));

            if (initAsset)
            {
                // TEMP: Init bnk was found, but caching isn't supported yet, prevent exception from throwing
                _completedWwiseFullBnkInit = true;
                Log.Debug($"Preloaded total of {totalLoadedBanks} soundbanks, loaded size {_totalLoadedWwiseSize}/{_totalWwiseBanksSize}");
                return;
            }
        }

        foreach (var wwiseFile in wwiseFiles)
        {
            string fullPath = wwiseFile.Path;
            string soundBankName = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            bool isWemFile = extension == ".wem";

            if (isWemFile)
            {
                if (uint.TryParse(soundBankName, out var wemId) && !_looseWemFilesLookup.ContainsKey(wemId))
                {
                    _looseWemFilesLookup[wemId] = new FGameFileDeferredByteData(wwiseFile);
                }

                continue;
            }

            if (!TryLoadAndCacheWwiseFile(wwiseFile))
                continue;

            totalLoadedBanks += 1;
        }

        Log.Debug($"Preloaded total of {totalLoadedBanks} soundbanks, loaded size {_totalLoadedWwiseSize}/{_totalWwiseBanksSize}");
        _completedWwiseFullBnkInit = totalLoadedBanks > 0;
    }

    private bool TryLoadAndCacheWwiseFile(GameFile? gameFile)
    {
        if (gameFile is null || !gameFile.TryRead(out var data) || data is not { Length: > 0 } bankData)
            return false;

        using var reader = new FByteArchive(gameFile.NameWithoutExtension, bankData);
        try
        {
            var wwiseReader = new WwiseReader(reader, new WwiseGameFileSource(gameFile));
            _totalLoadedWwiseSize += wwiseReader.LoadedSize;
            _totalWwiseBanksSize += wwiseReader.TotalSize;
            CacheWwiseFile(wwiseReader);
            if (wwiseReader.Header.SoundBankId is not 0) // .pck files don't contain SoundBankId so it's always 0
                _wwiseLoadedSoundBanks.Add(wwiseReader.Header.SoundBankId);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to cache Wwise sound bank file {0}", gameFile.Name);
            return false;
        }

        return true;
    }

    private void CacheWwiseFile(WwiseReader wwiseReader)
    {
        if (wwiseReader.AKPKBankEntries is { Count: > 0 } banks)
        {
            foreach (var bank in banks)
            {
                CacheWwiseFile(bank);
            }
        }

        if (wwiseReader.Hierarchies != null)
        {
            foreach (var h in wwiseReader.Hierarchies)
            {
                uint id = h.Data.Id;
                if (_wwiseHierarchyTables.TryGetValue(id, out var existing))
                {

                    if (!_wwiseHierarchyDuplicates.ContainsKey(id))
                        _wwiseHierarchyDuplicates[id] = [existing];

                    _wwiseHierarchyDuplicates[id].Add(h);
                }
                else
                {
                    _wwiseHierarchyTables[id] = h;
                }
            }
        }

        foreach (var kv in wwiseReader.WwiseEncodedMedias)
        {
            if (!_wwiseEncodedMedia.ContainsKey(kv.Key))
                _wwiseEncodedMedia[kv.Key] = kv.Value;
        }

        if (wwiseReader.WemFile?.IsValid is true)
            _wwiseEncodedMedia[wwiseReader.Path] = wwiseReader.WemFile; // wwiseReader.Path here needs to be wem file name!
    }

    private List<string> LoadWwisePackagingSettings()
    {
        var engineConfig = _provider.DefaultGame;
        if (engineConfig is null)
            return [];

        var values = new List<string>();
        engineConfig.EvaluatePropertyValues("/Script/AkAudio.AkSettings", "WwiseStagingDirectory", values);
        var path = values.FirstOrDefault()?.SubstringAfter("Path=\"").SubstringBefore("\")");
        if (!string.IsNullOrEmpty(path))
        {
            _baseWwiseAudioPath = Path.Combine(_provider.ProjectName, "Content", path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
        }
        values.Clear();
        engineConfig.EvaluatePropertyValues("/Script/WwisePackaging.WwisePackagingSettings", "AssetLibraries", values);

        return [.. values.Select(x => _provider.FixPath(x.TrimStart('/').SubstringBeforeLast('.') + ".uasset"))];
    }

    private void LoadMultiReferenceLibrary()
    {
        if (_loadedMultiRefLibrary)
            return;

        _loadedMultiRefLibrary = true;

        var configFiles = LoadWwisePackagingSettings();

        var assetFiles = _provider.Files.Values.Where(f =>
            f.Path.EndsWith("ReferenceAssetLibrary.uasset", StringComparison.OrdinalIgnoreCase) || configFiles.Contains(f.Path, _provider.PathComparer));
 
        foreach (var assetFile in assetFiles)
        {
            TryLoadMultiReferenceAsset(assetFile);
        }
    }

    private void TryLoadMultiReferenceAsset(GameFile? assetFile)
    {
        if (assetFile == null)
            return;

        try
        {
            var package = _provider.LoadPackage(assetFile.Path);

            var wwiseAssetLib = package.GetExports()
                .OfType<UWwiseAssetLibrary>()
                .FirstOrDefault();

            var loadedSize = 0L;
            var totalSize = 0L;
            if (wwiseAssetLib == null)
            {
                Log.Warning("No UWwiseAssetLibrary found in the package {0}", assetFile.Path);
                return;
            }

            if (wwiseAssetLib.CookedData?.PackagedFiles != null)
            {
                foreach (var pf in wwiseAssetLib.CookedData.PackagedFiles)
                {
                    if (pf.BulkData != null && !_multiReferenceLibraryCache.ContainsKey(pf.Hash))
                    {
                        CacheWwiseFile(pf.BulkData);
                        _multiReferenceLibraryCache[pf.Hash] = pf.BulkData;
                        loadedSize += pf.BulkData.LoadedSize;
                        totalSize += pf.BulkData.TotalSize;
                    }
                }
            }
            _totalLoadedWwiseSize += loadedSize;
            _totalWwiseBanksSize += totalSize;
            Log.Information("Loaded {Name} and cached {Count} packaged files, loaded size {size}/{total}", assetFile.Name,
                _multiReferenceLibraryCache.Count, _totalLoadedWwiseSize, _totalWwiseBanksSize);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load {Name}", assetFile.Name);
        }
    }

    private static string ResolveWwisePath(string path, FWwisePackagedFile? packagedFile, bool isPathNone)
    {
        if (isPathNone && (packagedFile == null || packagedFile.PathName.IsNone))
        {
            return string.Empty;
        }

        if (packagedFile != null && isPathNone && !packagedFile.PathName.IsNone)
        {
            path = packagedFile.PathName.ToString();
        }

        const string prefix = "WwiseAudio/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..]
            : path;
    }

    private IEnumerable<Hierarchy> GetHierarchiesById(uint id)
    {
        if (_wwiseHierarchyTables.TryGetValue(id, out var primary))
        {
            yield return primary;
        }

        if (_wwiseHierarchyDuplicates.TryGetValue(id, out var duplicates))
        {
            foreach (var duplicate in duplicates)
            {
                yield return duplicate;
            }
        }
    }
}

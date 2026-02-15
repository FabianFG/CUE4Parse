using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwiseExtractedSound
{
    public required string OutputPath { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => OutputPath + "." + Extension.ToLowerInvariant();
}

public class WwiseProviderConfiguration(long maxTotalWwiseSize = 4L * 1024 * 1024 * 1024, int maxBankFiles = 1024)
{
    // Important note: If game splits audio event hierarchies across multiple soundbanks or audio events don't reference soundbanks to load (that happens in older Wwise versions) and either of these limits is reached, given game requires custom loading implementation!
    public long MaxTotalWwiseSize { get; } = maxTotalWwiseSize;
    public int MaxBankFiles { get; } = maxBankFiles;

    public static WwiseProviderConfiguration GetFinalConfiguration(EGame game, WwiseProviderConfiguration? userConfig)
    {
        var baseConfig = userConfig ?? new WwiseProviderConfiguration();

        return game switch
        {
            EGame.GAME_AceCombat7 or EGame.GAME_DarkPicturesAnthologyHouseOfAshes or EGame.GAME_DarkPicturesAnthologyLittleHope
                or EGame.GAME_DarkPicturesAnthologyManofMedan or EGame.GAME_DarkPicturesAnthologyTheDevilinMe or EGame.GAME_TheQuarry => new WwiseProviderConfiguration(long.MaxValue, baseConfig.MaxBankFiles),
            _ => baseConfig
        };
    }
}

public partial class WwiseProvider
{
    private readonly AbstractVfsFileProvider _provider;
    private readonly WwiseProviderConfiguration _configuration;
    private readonly string _gameDirectory;
    private string? _baseWwiseAudioPath;

    private static readonly HashSet<string> _validWwiseExtensions = new(StringComparer.OrdinalIgnoreCase) { "bnk", "pck", "wem" };
    private readonly Dictionary<uint, Hierarchy> _wwiseHierarchyTables = [];
    private readonly Dictionary<uint, List<Hierarchy>> _wwiseHierarchyDuplicates = [];
    private readonly Dictionary<string, byte[]> _wwiseEncodedMedia = [];
    private readonly HashSet<uint> _wwiseLoadedSoundBanks = [];
    private readonly Dictionary<uint, WwiseReader> _multiReferenceLibraryCache = [];
    private bool _completedWwiseFullBnkInit = false;
    private bool _loadedMultiRefLibrary = false;

    private readonly record struct WwiseLocation(string Path, bool InProvider);
    private readonly Dictionary<uint, WwiseLocation> _looseWemFilesLookup = [];

    public WwiseProvider(AbstractVfsFileProvider provider, string gameDirectory, int maxBankFiles)
        : this(provider, gameDirectory, new WwiseProviderConfiguration(maxBankFiles: maxBankFiles))
    {
    }
    public WwiseProvider(AbstractVfsFileProvider provider, string gameDirectory, WwiseProviderConfiguration? configuration = null)
    {
        _provider = provider;
        _configuration = WwiseProviderConfiguration.GetFinalConfiguration(_provider.Versions.Game, configuration);
        _gameDirectory = gameDirectory;

        LoadMultiReferenceLibrary();

        if (_configuration.MaxBankFiles > 0)
        {
            BulkInitializeWwise();
            if (!_completedWwiseFullBnkInit)
                throw new InvalidOperationException("Failed to initialize Wwise soundbanks. Ensure that the provider has files to work with.");
        }
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
            results.Add(new WwiseExtractedSound
            {
                OutputPath = Path.Combine(ownerDirectory, media.Key),
                Extension = "wem",
                Data = media.Value,
            });
        }

        return results;
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

            foreach (var hierarchy in GetHierarchiesById(audioEventId))
            {
                if (hierarchy.Data is HierarchyEvent hierarchyEvent)
                {
                    LoopThroughEventActions(hierarchyEvent, results, _baseWwiseAudioPath, audioEvent.Name);
                }
            }

            return results;
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

        byte[] data = [];
        if (media.PackagedFile?.BulkData?.WemFile.Length > 0)
        {
            data = media.PackagedFile.BulkData.WemFile;
        }
        else if (_provider.TrySaveAsset(mediaRelativePath, out var providerData))
        {
            data = providerData;
        }
        else if (_multiReferenceLibraryCache.TryGetValue(media.PackagedFile?.Hash ?? 0, out var multiRefReader))
        {
            data = multiRefReader.WemFile;
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

    private void ProcessSoundBankCookedData(FWwiseSoundBankCookedData soundBank, FWwiseEventCookedData? eventData, List<WwiseExtractedSound> results)
    {
        if (!soundBank.bContainsMedia)
            return;

        DetermineBaseWwiseAudioPath();

        var soundBankName = ResolveWwisePath(soundBank.SoundBankPathName.Text, soundBank.PackagedFile, soundBank.SoundBankPathName.IsNone);
        var soundBankPath = Path.Combine(_baseWwiseAudioPath, soundBankName);
        TryLoadAndCacheWwiseFile(soundBankPath, soundBankName, (uint) soundBank.SoundBankId, out _);

        if (_wwiseHierarchyTables.TryGetValue((uint) eventData!.Value.EventId, out var eventHierarchy) &&
            eventHierarchy.Data is HierarchyEvent hierarchyEvent)
        {
            LoopThroughEventActions(hierarchyEvent, results, soundBankPath.SubstringBeforeLast('.'), eventData.Value.DebugName.Text);
        }
    }

    private void LoadSoundBankById(uint soundBankId)
    {
        if (_wwiseLoadedSoundBanks.Contains(soundBankId))
            return;

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
                using var headerReader = file.Value.CreateReader();
                var id = WwiseReader.TryReadSoundBankId(headerReader);
                if (id != soundBankId)
                    continue;

                using var fullReader = file.Value.CreateReader();
                var reader = new WwiseReader(fullReader);
                CacheWwiseFile(reader);
                _wwiseLoadedSoundBanks.Add(soundBankId);
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to read soundbank file '{file.Key}'");
            }
        }
    }

    private void LoopThroughEventActions(HierarchyEvent hierarchyEvent, List<WwiseExtractedSound> results, string ownerDirectory, string? debugName = null)
    {
        foreach (var actionId in hierarchyEvent.EventActionIds)
        {
            if (!_wwiseHierarchyTables.TryGetValue(actionId, out var actionHierarchy) ||
                actionHierarchy.Data is not HierarchyEventAction eventAction)
                continue;

            // TODO: If EventActionPlay points to different soundbank ID than we're currently in, use `wwiseReader.IdToString` to convert to bank name, serialize it, and continue traversing from there
            // This isn't needed if all soundbanks are loaded anyway

            //if (eventAction.EventActionType == EEventActionType.Play)
            //{
            //    var playActionData = (AkActionPlay) eventAction.ActionData;
            //    var bankId = playActionData.BankId;
            //    if (bankId != referencedSoundBankId) // I need to know what soundbank I'm currently in
            //    {
            //        var soundbankConvertedName = IdToString[referencedSoundBankId]; // I need IdToString from given soundbank
            //        TryLoadAndCacheSoundBank(Path.Combine(baseWwiseAudioPath, soundbankConvertedName + ".bnk"), soundbankConvertedName, out _);
            //    }
            //}

            TraverseAndSave(eventAction.ReferencedId);
        }

        void TraverseAndSave(uint id)
        {
            foreach (var hierarchy in GetHierarchiesById(id))
            {
                if (!_visitedHierarchies.Add((id, hierarchy)))
                    continue;

                switch (hierarchy.Data)
                {
                    case HierarchySoundSfxVoice soundSfx:
                        SaveWemSound(soundSfx.Source.SourceId);
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
                        foreach (var childId in switchContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchyLayerContainer layerContainer:
                        foreach (var childId in layerContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchyActorMixer mixerContainer:
                        foreach (var childId in mixerContainer.ChildIds)
                            TraverseAndSave(childId);
                        break;
                    case HierarchyEvent eventContainer:
                        foreach (var actionId in eventContainer.EventActionIds)
                        {
                            if (!_wwiseHierarchyTables.TryGetValue(actionId, out var actionHierarchy) ||
                                actionHierarchy.Data is not HierarchyEventAction eventAction)
                                continue;

                            TraverseAndSave(eventAction.ReferencedId);
                        }
                        break;
                    default:
                        Log.Warning("Unhandled hierarchy type {0}, while traversing through EventActions", hierarchy.Type);
                        break;
                }
            }
        }

        void SaveWemSound(uint wemId)
        {
            if (!_visitedWemIds.Add(wemId))
                return;

            if (_looseWemFilesLookup.TryGetValue(wemId, out var location))
            {
                var name = Path.GetFileNameWithoutExtension(location.Path);
                TryLoadAndCacheWwiseFile(location.Path, name, 0, out var size, loadFromFileSystem: !location.InProvider, isWemFile: true);
            }

            var fileName = wemId.ToString();
            if (_wwiseEncodedMedia.TryGetValue(fileName, out var wemData))
            {
                if (!string.IsNullOrEmpty(debugName))
                    fileName = $"{debugName} ({fileName})";

                var outputPath = Path.Combine(ownerDirectory, fileName);
                if (outputPath.StartsWith('/'))
                    outputPath = outputPath[1..];

                results.Add(new WwiseExtractedSound
                {
                    OutputPath = outputPath.Replace('\\', '/'),
                    Extension = "wem",
                    Data = wemData,
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
                _looseWemFilesLookup[wemId] = new WwiseLocation(wem, InProvider: false);
        }

        var loadedBanks = 0;
        foreach (var bnk in wwiseBankFiles)
        {
            var name = Path.GetFileNameWithoutExtension(bnk);
            if (TryLoadAndCacheWwiseFile(bnk, name, 0, out var size, loadFromFileSystem: true, isWemFile: false))
                loadedBanks++;
        }
        return loadedBanks;
    }

    private void BulkInitializeWwise()
    {
        if (_completedWwiseFullBnkInit)
            return;

        long totalLoadedSize = 0;
        int totalLoadedBanks = LoadExternalWwiseFiles();

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
                return;
            }
        }

        foreach (var wwiseFile in wwiseFiles)
        {
            string fullPath = wwiseFile.Path;
            string soundBankName = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            bool isWemFile = extension == ".wem";

            if (totalLoadedBanks >= _configuration.MaxBankFiles)
            {
#if DEBUG
                Log.Debug("Reached maximum number of Wwise files to load. This game might require custom loading implementation (only necessary under some special cases).");
#endif
                break;
            }

            if (isWemFile)
            {
                if (uint.TryParse(soundBankName, out var wemId) && !_looseWemFilesLookup.ContainsKey(wemId))
                    _looseWemFilesLookup[wemId] = new WwiseLocation(fullPath, InProvider: true);
                
                continue;
            }

            if (!TryLoadAndCacheWwiseFile(fullPath, soundBankName, 0, out var size))
                continue;

            if (totalLoadedSize + size > _configuration.MaxTotalWwiseSize)
            {
#if DEBUG
                Log.Debug("Reached maximum total size of Wwise files to load. This game might require custom loading implementation (only necessary under some special cases).");
#endif
                break;
            }

            totalLoadedSize += size;
            totalLoadedBanks += 1;
        }

        Log.Debug($"Preloaded total of {totalLoadedBanks} soundbanks");
        _completedWwiseFullBnkInit = totalLoadedBanks > 0;
    }

    private bool TryLoadAndCacheWwiseFile(string fullAbsolutePath, string soundBankName, uint soundBankId, out long fileSize, bool loadFromFileSystem = false, bool isWemFile = false)
    {
        fileSize = 0;

        if (isWemFile && uint.TryParse(soundBankName, out uint parsedId))
            soundBankId = parsedId;
        if (_wwiseLoadedSoundBanks.Contains(soundBankId))
            return false;

        byte[]? data;
        if (loadFromFileSystem)
        {
            if (!File.Exists(fullAbsolutePath))
                return false;

            try
            {
                data = File.ReadAllBytes(fullAbsolutePath);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to read file {fullAbsolutePath}: {ex.Message}");
                return false;
            }
        }
        else
        {
            if (!_provider.TrySaveAsset(fullAbsolutePath, out data))
                return false;
        }

        using var archive = new FByteArchive(soundBankName, data);
        var wwiseReader = new WwiseReader(archive);
        CacheWwiseFile(wwiseReader);

        if (wwiseReader.Header.SoundBankId is not 0) // .pck files don't contain SoundBankId so it's always 0
            _wwiseLoadedSoundBanks.Add(isWemFile ? soundBankId : wwiseReader.Header.SoundBankId);

        fileSize = data.LongLength;
        return true;
    }

    private void CacheWwiseFile(WwiseReader wwiseReader)
    {
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

        if (wwiseReader.WemFile.Length > 0)
            _wwiseEncodedMedia[wwiseReader.Path] = wwiseReader.WemFile; // wwiseReader.Path here needs to be wem file name!
    }

    private void LoadMultiReferenceLibrary()
    {
        if (_loadedMultiRefLibrary)
            return;

        _loadedMultiRefLibrary = true;

        var assetFiles = _provider.Files.Values.Where(f =>
            f.Path.EndsWith("ReferenceAssetLibrary.uasset", StringComparison.OrdinalIgnoreCase));

        foreach (var assetFile in assetFiles)
        {
            TryLoadMultiReferenceAsset(assetFile);
        }
    }

    private void TryLoadMultiReferenceAsset(FileProvider.Objects.GameFile? assetFile)
    {
        if (assetFile == null)
            return;

        try
        {
            var package = _provider.LoadPackage(assetFile.Path);

            var wwiseAssetLib = package.GetExports()
                .OfType<UWwiseAssetLibrary>()
                .FirstOrDefault();

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
                    }
                }
            }

            Log.Information("Loaded {Name} and cached {Count} packaged files", assetFile.Name,
                _multiReferenceLibraryCache.Count);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load {Nmae}", assetFile.Name);
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

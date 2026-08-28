using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;
using CUE4Parse.Utils;

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
    private string _baseWwiseAudioPath;

    private static readonly HashSet<string> _validWwiseExtensions = new(StringComparer.OrdinalIgnoreCase) { "bnk", "pck", "wem" };
    private readonly Dictionary<uint, Hierarchy> _wwiseHierarchyTables = [];
    private readonly Dictionary<uint, List<Hierarchy>> _wwiseHierarchyDuplicates = [];
    private readonly Dictionary<(EAkGroupType Type, uint GroupId), List<uint>> _musicSwitchContainersByGameSync = [];
    private readonly Dictionary<string, FDeferredByteData> _wwiseEncodedMedia = [];
    private readonly HashSet<uint> _wwiseLoadedSoundBanks = [];
    private readonly Dictionary<uint, WwiseReader> _multiReferenceLibraryCache = [];
    private bool _loadedMultiRefLibrary = false;
    private long _totalLoadedWwiseSize = 0L;
    private long _totalWwiseBanksSize = 0L;

    private readonly Dictionary<uint, FGameFileDeferredByteData> _looseWemFilesLookup = [];

    public WwiseProvider(AbstractVfsFileProvider provider, string gameDirectory)
    {
        _provider = provider;
        _gameDirectory = gameDirectory;
        _baseWwiseAudioPath = Path.Combine(_provider.ProjectName, "Content", "WwiseAudio");

        LoadMultiReferenceLibrary();

        if (!BulkInitializeWwise())
            throw new InvalidOperationException("Failed to initialize Wwise soundbanks. Ensure that the provider has files to work with.");
    }

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
        var soundBankCookedData = audioBank.SoundBankCookedData;
        if (soundBankCookedData is null)
        {
            var soundBankId = audioBank.GetOrDefault<uint>("ShortID", comparisonType: StringComparison.OrdinalIgnoreCase);
            if (soundBankId is 0)
                return [];

            var soundBank = LoadSoundBankById(soundBankId, returnBank: true);
            if (soundBank is null)
                return [];

            return ExtractBankSounds(soundBank);
        }
        else
        {
            var ownerDirectory = GetOwnerDirectory(audioBank);
            var results = new List<WwiseExtractedSound>();
            foreach (var eventName in soundBankCookedData.IncludedEventNames)
            {
                if (eventName.IsNone)
                    continue;

                var audioEventId = WwiseFnv.GetHash(eventName.Text);
                LoopThroughEvent(audioEventId, results, ownerDirectory, eventName.Text);
            }

            return results;
        }
    }

    public List<WwiseExtractedSound> ExtractAudioEventSounds(UAkAudioEvent audioEvent)
    {
        var results = new List<WwiseExtractedSound>();

        var ownerDirectory = GetOwnerDirectory(audioEvent);
        var wwiseData = audioEvent.EventCookedData;
        if (wwiseData == null)
        {
            var audioEventId = audioEvent.GetOrDefault<uint>("ShortID", comparisonType: StringComparison.OrdinalIgnoreCase);
            if (audioEventId == 0)
                audioEventId = WwiseFnv.GetHash(audioEvent.Name);

            var requiredBank = audioEvent.GetOrDefault<UObject?>("RequiredBank", comparisonType: StringComparison.OrdinalIgnoreCase);
            var soundBankId = requiredBank?.GetOrDefault<uint>("ShortID", comparisonType: StringComparison.OrdinalIgnoreCase) ?? 0;
            if (soundBankId != 0)
                LoadSoundBankById(soundBankId);

            LoopThroughEvent(audioEventId, results, ownerDirectory, audioEvent.Name);

            return results;
        }

        // cache all banks first
        foreach (var (languageData, eventData) in wwiseData.Value.EventLanguageMap)
        {
            if (!eventData.HasValue)
                continue;

            foreach (var media in eventData.Value.Media)
            {
                CacheMediaCookedData(media);
            }

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

        // Track what's in media first so we don't resolve the same audio twice via event resolution
        var visitedMedia = new HashSet<uint>();
        foreach (var (languageData, eventData) in wwiseData.Value.EventLanguageMap)
        {
            if (!eventData.HasValue)
                continue;

            foreach (var media in eventData.Value.Media)
            {
                if (!visitedMedia.Add(media.MediaId))
                    continue;
                ProcessMediaCookedData(ownerDirectory, media, languageData, results);
            }

            foreach (var leaf in eventData.Value.SwitchContainerLeaves)
            {
                foreach (var media in leaf.Media)
                {
                    if (!visitedMedia.Add(media.MediaId))
                        continue;
                    ProcessMediaCookedData(ownerDirectory, media, languageData, results);
                }
            }
        }

        foreach (var (languageData, eventData) in wwiseData.Value.EventLanguageMap)
        {
            if (!eventData.HasValue)
                continue;

            foreach (var soundBank in eventData.Value.SoundBanks)
            {
                ProcessSoundBankCookedData(ownerDirectory, eventData, results, visitedMedia);
            }

            foreach (var leaf in eventData.Value.SwitchContainerLeaves)
            {
                foreach (var soundBank in leaf.SoundBanks)
                {
                    ProcessSoundBankCookedData(ownerDirectory, eventData, results, visitedMedia);
                }
            }
        }

        return results;
    }

    private void ProcessMediaCookedData(string ownerDirectory, FWwiseMediaCookedData media, FWwiseLanguageCookedData languageData, List<WwiseExtractedSound> results)
    {
        var mediaPath = media.MediaPathName.IsNone
            ? media.PackagedFile?.PathName.ToString() ?? string.Empty
            : media.MediaPathName.Text;
        var wemFileName = Path.GetFileNameWithoutExtension(mediaPath);

        FDeferredByteData? data = media switch
        {
            _ when media.PackagedFile?.BulkData?.WemFile is { IsValid: true } mediaData
                => mediaData,
            _ when _looseWemFilesLookup.TryGetValue(media.MediaId, out var wemGameFile)
                => wemGameFile,
            _ when _wwiseEncodedMedia.TryGetValue(wemFileName, out var wemData)
                => wemData,
            _ when _multiReferenceLibraryCache.TryGetValue(media.PackagedFile?.Hash ?? 0, out var multiRefReader)
                => multiRefReader?.WemFile,
            _ => null
        };

        if (data is null)
            Log.Error("Failed to load data for '{WemFileName}' wem loose file", wemFileName);

        var mediaDebugName = !string.IsNullOrEmpty(media.DebugName.Text) && !media.DebugName.IsNone
            ? media.DebugName.Text.SubstringBeforeLast('.')
            : wemFileName;

        var namedPath = Path.Combine(
            ownerDirectory,
            $"{mediaDebugName} ({languageData.LanguageName.Text})"
        );

        results.Add(new WwiseExtractedSound
        {
            OutputPath = namedPath.Replace('\\', '/'),
            Extension = "wem",
            Data = data,
        });
    }

    private void ProcessSoundBankCookedData(string ownerDirectory, FWwiseEventCookedData? eventData, List<WwiseExtractedSound> results, HashSet<uint> visitedMedia) =>
        LoopThroughEvent(eventData!.Value.EventId, results, ownerDirectory, visitedMedia, eventData.Value.DebugName.Text);

    private void CacheSoundBankCookedData(FWwiseSoundBankCookedData soundBank)
    {
        var bulkPackagedSoundBank = soundBank.PackagedFile?.BulkData;
        if (bulkPackagedSoundBank is not null && !_wwiseLoadedSoundBanks.Contains(bulkPackagedSoundBank.Header.SoundBankId))
        {
            CacheWwiseFile(bulkPackagedSoundBank);
            _wwiseLoadedSoundBanks.Add(bulkPackagedSoundBank.Header.SoundBankId);
        }
    }

    private void CacheMediaCookedData(FWwiseMediaCookedData media)
    {
        var bulkPackagedMedia = media.PackagedFile?.BulkData;
        if (bulkPackagedMedia?.WemFile?.IsValid is true)
        {
            _wwiseEncodedMedia[media.MediaId.ToString()] = bulkPackagedMedia.WemFile;
        }
    }

    private WwiseReader? LoadSoundBankById(uint soundBankId, bool returnBank = false)
    {
        if (!returnBank && _wwiseLoadedSoundBanks.Contains(soundBankId))
            return null;

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
                var wwiseAr = new FWwiseArchive(reader);
                var soundBank = new WwiseReader(wwiseAr, new WwiseGameFileSource(file.Value));
                CacheWwiseFile(soundBank);
                _wwiseLoadedSoundBanks.Add(soundBankId);
                return soundBank;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to read soundbank file '{FileName}'", file.Key);
            }
        }

        Log.Warning("Soundbank with ID {ID} wasn't found", soundBankId);

        return null;
    }

    private void LoopThroughEvent(uint eventId, List<WwiseExtractedSound> results, string ownerDirectory, string? debugName = null) =>
        LoopThroughEvent(eventId, results, ownerDirectory, [], debugName);
    private void LoopThroughEvent(uint eventId, List<WwiseExtractedSound> results, string ownerDirectory, HashSet<uint> visitedMedia, string? debugName = null) =>
        new EventSoundResolver(this, results, ownerDirectory, debugName, visitedMedia).Resolve(eventId);

    private int LoadExternalWwiseFiles()
    {
        var searchDirectory = _gameDirectory;
        var dir = new DirectoryInfo(searchDirectory);
        if (!dir.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (Directory.GetParent(searchDirectory) is { } parentInfo)
            searchDirectory = parentInfo.FullName;

        var wwiseDir = Directory.EnumerateDirectories(searchDirectory, "WwiseAudio", SearchOption.AllDirectories).FirstOrDefault(Directory.Exists);
        if (wwiseDir is null)
        {
            Log.Warning("Wwise directory not found under '{SearchDirectory}', external Wwise files might not exist", searchDirectory);
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

    private bool BulkInitializeWwise()
    {
        int totalLoadedBanks = _multiReferenceLibraryCache.Count;
        totalLoadedBanks += LoadExternalWwiseFiles();

        var wwiseFiles = _provider.Files.Values
            .Where(file => _validWwiseExtensions.Contains(file.Extension))
            // We need to prioritize .pck over .bnk (if there's such pair, .bnk might contain only partial audio buffer (prefetch), full one is stored in .pck)
            .OrderByDescending(file => file.Extension.Equals("pck", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var wwiseFile in wwiseFiles)
        {
            var fullPath = wwiseFile.Path;
            var soundBankName = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var isWemFile = extension == ".wem";
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

        Log.Debug("Preloaded total of {LoadedBankCount} soundbanks, loaded size {LoadedSize}/{TotalSize}", totalLoadedBanks, _totalLoadedWwiseSize.GetReadableSize(), _totalWwiseBanksSize.GetReadableSize());
        return totalLoadedBanks > 0 || UsesBulkDataPackaging();
    }

    // BulkData packaging stores banks inlined, simply check if InitBank exists
    private bool UsesBulkDataPackaging() =>
        _provider.Files.Values.Any(file =>
            file.Extension.Equals("uasset", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileNameWithoutExtension(file.Path) is var name &&
            (name.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
             name.Equals("InitBank", StringComparison.OrdinalIgnoreCase)));

    private bool TryLoadAndCacheWwiseFile(GameFile? gameFile)
    {
        if (gameFile is null || !gameFile.TryRead(out var data) || data is not { Length: > 0 })
            return false;

        using var reader = new FWwiseArchive(gameFile.NameWithoutExtension, data);
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
            foreach (var hirc in wwiseReader.Hierarchies)
            {
                if (hirc.Data is HierarchyMusicSwitchContainer musicSwitchContainer)
                    IndexMusicSwitchContainer(musicSwitchContainer);

                // Not needed for resolving audio
                if (hirc.Type is EAKBKHircType.AudioBus or EAKBKHircType.ActorMixer)
                    continue;

                uint id = hirc.Data.Id;
                if (_wwiseHierarchyTables.TryGetValue(id, out var existing))
                {

                    if (!_wwiseHierarchyDuplicates.ContainsKey(id))
                        _wwiseHierarchyDuplicates[id] = [existing];

                    _wwiseHierarchyDuplicates[id].Add(hirc);
                }
                else
                {
                    _wwiseHierarchyTables[id] = hirc;
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

    private void IndexMusicSwitchContainer(HierarchyMusicSwitchContainer container)
    {
        foreach (var argument in container.Arguments)
        {
            var key = (argument.GroupType, argument.GroupId);
            if (!_musicSwitchContainersByGameSync.TryGetValue(key, out var containerIds))
            {
                _musicSwitchContainersByGameSync[key] = containerIds = [];
            }

            if (!containerIds.Contains(container.Id))
            {
                containerIds.Add(container.Id);
            }
        }
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

            var filesCount = 0;
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
                        filesCount++;
                        loadedSize += pf.BulkData.LoadedSize;
                        totalSize += pf.BulkData.TotalSize;
                    }
                }
            }
            _totalLoadedWwiseSize += loadedSize;
            _totalWwiseBanksSize += totalSize;
            Log.Information("Loaded {Name} and cached {Count} packaged files, loaded size {LoadedSize}/{TotalSize}", assetFile.Name, filesCount, loadedSize.GetReadableSize(), totalSize.GetReadableSize());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load {Name}", assetFile.Name);
        }
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

    public string GetOwnerDirectory(UObject obj)
    {
        var path = _provider.FixPath(obj.Owner?.Name ?? _baseWwiseAudioPath);
        return Path.GetDirectoryName(path) ?? _baseWwiseAudioPath;
    }
}

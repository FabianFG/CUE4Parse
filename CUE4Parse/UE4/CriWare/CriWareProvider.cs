using System.Security.Cryptography;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.CriWare.Decoders;
using CUE4Parse.UE4.CriWare.Readers;
using CUE4Parse.UE4.Objects.UObject;
using UE4Config.Parsing;

namespace CUE4Parse.UE4.CriWare;

public class CriWareExtractedSound
{
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => Name + "." + Extension.ToLowerInvariant();
}

/// <summary>
/// Tested games:
///
/// 4.20 | DAEMON X MACHINA
/// 4.23 | SgyuinBaldo
/// 4.27 | DRAGON QUEST I & II HD-2D Remake, EDENS ZERO, MOBILE SUIT GUNDAM SEED BATTLE DESTINY REMASTERED
///      | Persona 3 Reload (0x0000000000B5DE48)
/// 5.1  | DRAGON BALL: Sparking! ZERO (0xB7B8B9442F99A221), Jujutsu Kaisen Cursed Clash (0x0DAA5EA10B547CDE)
///      | SAND LAND (0x0CA47CCB51010000), SWORD ART ONLINE Fractured Daydream
/// 5.3  | Demon Slayer -Kimetsu no Yaiba- The Hinokami Chronicles 2
/// 5.4  | Double Dragon Revive, FANTASY LIFE i: The Girl Who Steals Time
///      | Rune Factory: Guardians of Azuma, Sonic Racing: CrossWorlds (0x00720FB46101DF7A)
///
/// </summary>
public class CriWareProvider
{
    private readonly record struct AwbLocation(string Path, bool InProvider);
    private Dictionary<string, List<AwbLocation>> _streamingAwbLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AwbLocation> _streamingAwbHashLookup = [];

    private readonly IFileProvider _provider;
    private readonly string _gameDirectory;
    private string? _criWareContentDir;

    public CriWareProvider(IFileProvider provider, string gameDirectory)
    {
        _provider = provider;
        _gameDirectory = gameDirectory;
        var dir = new DirectoryInfo(_gameDirectory);
        if (dir.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase) && Directory.GetParent(_gameDirectory) is { } parentInfo)
            _gameDirectory = parentInfo.FullName;
        LoadCriWareConfig(provider);
        CreateAwbLookupTable(provider);
    }

    public List<CriWareExtractedSound> ExtractCriWareSounds(AcbReader acb, string acbName)
        => ExtractCriWareSoundsInternal(acb, null, acbName);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AwbReader awb, string awbName)
        => ExtractFromAwb(awb, null, null, awbName);
    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomWaveBank awb)
    {
        if (awb?.AtomWaveBankData == null)
            return [];

        return ExtractFromAwb(awb.AtomWaveBankData, null, null, awb.Name);
    }
    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCueSheet cueSheet)
    {
        var awbDirectory = cueSheet.Properties
            .FirstOrDefault(p => p.Name.Text == "AwbDirectory")
            ?.Tag?.GetValue<FStructFallback>();

        if (awbDirectory?.TryGetValue(out string awbDir, "Path") == true)
            CreateAwbLookupTable(_provider, awbDir);

        return ExtractCriWareSoundsInternal(cueSheet.AcbReader, null, cueSheet.Name);
    }
    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomCueSheet cueSheet)
    {
        var waveBanks = cueSheet.Properties.FirstOrDefault(p => p.Name.Text == "WaveBanks");

        AwbReader? awb = null;
        if (waveBanks?.Tag is ArrayProperty waveBankArray && waveBankArray.Value != null)
        {
            foreach (var waveBankEntry in waveBankArray.Value.Properties)
            {
                var atomWaveBank = waveBankEntry.GetValue<UAtomWaveBank>();

                if (atomWaveBank == null)
                    continue;

                awb = atomWaveBank.AtomWaveBankData;
            }
        }

        return ExtractCriWareSoundsInternal(cueSheet.AcbReader, awb, cueSheet.Name);
    }
    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCue soundAtomCue)
    {
        var results = new List<CriWareExtractedSound>();
        var cueName = soundAtomCue.GetOrDefault<string>("CueName");
        var cueSheet = soundAtomCue.GetOrDefault<FPackageIndex>("CueSheet");

        if (cueSheet?.TryLoad<USoundAtomCueSheet>(out var atomCueSheet) == true && atomCueSheet.AcbReader is { } acb)
        {
            var cueNameTable = acb.AtomCueSheetData["CueName"];
            var cueNameRow = cueNameTable.FirstOrDefault(cue =>
                cue["CueName"] is string name && string.Equals(name, cueName, StringComparison.OrdinalIgnoreCase));

            if (cueNameRow == null)
                return results;

            int cueIndex = Convert.ToInt32(cueNameRow["CueIndex"]);
            var cueRow = acb.AtomCueSheetData["Cue"][cueIndex];

            int cueId = Convert.ToInt32(cueRow["CueId"]);
            var waveforms = acb.GetWaveformsFromCueId(cueId);
            if (waveforms.Count == 0)
                return results;

            var memoryAwb = acb.GetAwb();
            var streamingAwb = LoadStreamingAwb(acb);

            var index = 0;
            foreach (var wave in waveforms)
            {
                if (wave.EncodeType is not (EEncodeType.HCA or EEncodeType.HCA_ALT))
                {
                    Log.Warning("Skipping waveform extraction. Waveform encoding type '{EncodingType}' is not supported", wave.EncodeType);
                    continue;
                }

                var hcaData = TryGetAudioData(memoryAwb, streamingAwb, wave);
                if (hcaData == null || hcaData.Length == 0)
                    continue;

                results.Add(
                    new CriWareExtractedSound
                    {
                        Name = waveforms.Count == 1 ? cueName : $"{cueName}_{index++:D4}",
                        Extension = "hca",
                        Data = hcaData
                    }
                );
            }
        }

        return results;
    }

    private List<CriWareExtractedSound> ExtractCriWareSoundsInternal(AcbReader? acb, AwbReader? streamingAwb, string cueSheetName)
    {
        if (acb == null)
            return [];

        var memoryAwb = acb.GetAwb();
        streamingAwb ??= LoadStreamingAwb(acb);

        return ExtractFromAwb(memoryAwb, streamingAwb, acb, cueSheetName);
    }

    private List<CriWareExtractedSound> ExtractFromAwb(AwbReader? memoryAwb, AwbReader? streamingAwb, AcbReader? acb, string baseName)
    {
        var results = new List<CriWareExtractedSound>();
        var visitedWaveforms = new HashSet<Waveform>();

        if (acb != null)
        {
            var cueTable = acb.AtomCueSheetData["Cue"];
            var cueNameTable = acb.AtomCueSheetData["CueName"];

            foreach (var cueRow in cueTable)
            {
                int cueId = Convert.ToInt32(cueRow["CueId"]);
                var waveforms = acb.GetWaveformsFromCueId(cueId);
                var cueNameRow = cueNameTable.FirstOrDefault(cue => Convert.ToInt32(cue["CueIndex"]) == cueId);
                var name = cueNameRow != null && cueNameRow["CueName"] is string cueName
                    ? cueName
                    : $"{Path.GetFileNameWithoutExtension(baseName)}_{cueId:D4}";

                var index = 0;
                foreach (var wave in waveforms)
                {
                    if (!visitedWaveforms.Add(wave))
                        continue;
                    if (!TryGetSupportedExtension(wave.EncodeType, out var extension))
                    {
                        Log.Warning("Skipping waveform extraction. Waveform encoding type '{EncodingType}' is not supported", wave.EncodeType);
                        continue;
                    }

                    var audioData = TryGetAudioData(memoryAwb, streamingAwb, wave);
                    if (audioData == null || audioData.Length == 0)
                        continue;

                    results.Add(new CriWareExtractedSound
                    {
                        Name = waveforms.Count == 1 ? name : $"{name}_{index++:D4}",
                        Extension = extension,
                        Data = audioData
                    });
                }
            }

            int waveformsCount = memoryAwb?.Waves.Count ?? 0 + streamingAwb?.Waves.Count ?? 0;
            if (visitedWaveforms.Count < waveformsCount)
            {
                Log.Warning("Not all waveforms were extracted from ACB '{AcbName}'. Extracted {ExtractedCount} out of {WaveformCount}.", baseName, visitedWaveforms.Count, waveformsCount);
            }
        }
        else
        {
            // If we want to extract directly from AWB
            // Audio is never played directly through AWB so we can't know what audio encoding was used nor what's proper audio name
            for (int i = 0; i < memoryAwb?.Waves.Count; i++)
            {
                var wave = memoryAwb.Waves[i];

                using var waveStream = memoryAwb.GetWaveSubfileStream(wave);
                if (waveStream.Length == 0)
                    continue;

                string waveName = $"{Path.GetFileNameWithoutExtension(baseName)}_{wave.WaveId:D4}";

                var hcaData = waveStream.EmbedSubKey(memoryAwb.Subkey);

                results.Add(new CriWareExtractedSound
                {
                    Name = waveName,
                    Extension = "hca", // Most common extension, we can't know what's correct one without ACB
                    Data = hcaData
                });
            }
        }

        return results;
    }

    private static bool TryGetSupportedExtension(EEncodeType encodeType, out string extension)
    {
        switch (encodeType)
        {
            case EEncodeType.HCA:
            case EEncodeType.HCA_ALT:
                extension = "hca";
                return true;
            case EEncodeType.ADX:
                extension = "adx";
                return true;
            default:
                extension = null!;
                return false;
        }
    }

    private static byte[]? TryGetAudioData(AwbReader? awb, AwbReader? streamingAwb, Waveform waveform)
    {
        (AwbReader? reader, ushort waveId) = waveform.Streaming switch
        {
            EWaveformStreamType.Memory => (awb, waveform.Id),
            EWaveformStreamType.Streaming or EWaveformStreamType.Both => (streamingAwb, waveform.StreamId),
            _ => (null, 0),
        };

        if (reader == null)
            return null;

        var wave = reader.Waves.FirstOrDefault(w => w.WaveId == waveId);
        using var waveStream = reader.GetWaveSubfileStream(wave);

        return waveStream.EmbedSubKey(reader.Subkey);
    }

    private AwbReader? LoadStreamingAwb(AcbReader acb)
    {
        AwbReader? awb = null;
        var hash = acb.TryGetTableValue<byte[]>("StreamAwb", "Hash");
        if (hash != null)
        {
            var awbName = Path.ChangeExtension(acb.Name, ".awb");
            _streamingAwbLookup.TryGetValue(awbName, out var locations);

            AwbLocation? awbLocation = locations?.Count == 1
                ? locations[0]
                : FindAwbByHash(locations ?? _streamingAwbLookup.Values.SelectMany(static value => value), hash);

            if (awbLocation is null)
                return null;

            if (awbLocation.Value.InProvider)
            {
                if (!_provider.TryGetGameFile(awbLocation.Value.Path, out var gameFile) || !gameFile.TryCreateReader(out var reader))
                    return null;

                awb = new AwbReader(reader);
            }
            else
            {
                Stream awbStream = File.OpenRead(awbLocation.Value.Path);
                awbStream = CriWareAwbDecryption.Wrap(awbStream, awbLocation.Value.Path, _provider.Versions.Game);
                awb = new AwbReader(awbStream);
            }
        }

        return awb;
    }

    private void LoadCriWareConfig(IFileProvider provider)
    {
        if (!provider.TryGetGameFile("/Game/Config/DefaultEngine.ini", out var defaultEngine))
            return;

        var engineConfig = new ConfigIni(nameof(defaultEngine));

        if (defaultEngine.TryCreateReader(out var engineAr))
        {
            using (engineAr)
                engineConfig.Read(new StreamReader(engineAr));
        }

        var criwareSection = engineConfig.Sections
            .FirstOrDefault(s => s.Name == "/Script/CriWareRuntime.CriWarePluginSettings");

        var token = criwareSection?.Tokens
            .OfType<InstructionToken>()
            .FirstOrDefault(t => t.Key == "ContentDir");

        if (!string.IsNullOrEmpty(token?.Value))
        {
            _criWareContentDir = token.Value.Replace('\\', '/');
            Log.Information("CriWare content directory found at: {ContentDirectory}", token.Value);
        }
    }

    public void CreateAwbLookupTable(IFileProvider provider, string? overrideAwbDir = null)
    {
        if (_streamingAwbLookup.Count != 0)
            return;
        if (string.IsNullOrEmpty(_criWareContentDir) && string.IsNullOrEmpty(overrideAwbDir))
            return;

        var awbLookup = new Dictionary<string, List<AwbLocation>>(StringComparer.OrdinalIgnoreCase);

        void AddAwb(string path, bool inProvider)
        {
            var name = Path.GetFileName(path);
            if (!awbLookup.TryGetValue(name, out var locations))
                awbLookup[name] = locations = [];
            locations.Add(new AwbLocation(path, inProvider));
        }

        var searchDirs = new List<string>(2);
        if (!string.IsNullOrEmpty(_criWareContentDir))
            searchDirs.Add(_criWareContentDir);
        if (!string.IsNullOrEmpty(overrideAwbDir))
            searchDirs.Add(overrideAwbDir);

        // From file system
        var awbFiles = Directory.EnumerateFiles(_gameDirectory, "*.awb", SearchOption.AllDirectories)
            .Where(f => searchDirs.Any(d => f.Replace('\\', '/').Contains(d)));

        foreach (var file in awbFiles)
            AddAwb(file, false);

        // From provider
        var providerAwbFiles = provider.Files
            .Where(kv => kv.Key.EndsWith(".awb", StringComparison.OrdinalIgnoreCase) && searchDirs.Any(d => kv.Key.Replace('\\', '/').Contains(d)));

        foreach (var (path, _) in providerAwbFiles)
            AddAwb(path, true);

        _streamingAwbLookup = awbLookup;
    }

    // Searching by AWB hash is slow although it's actually the proper way to do it
    // only do it as a last resort when there are multiple AWBs with the same name or there's no match
    private AwbLocation? FindAwbByHash(IEnumerable<AwbLocation> locations, byte[] hash)
    {
        var hashString = Convert.ToHexString(hash);
        if (_streamingAwbHashLookup.TryGetValue(hashString, out var cachedLocation))
            return cachedLocation;

        foreach (var location in locations)
        {
            Stream stream;
            if (location.InProvider)
            {
                if (!_provider.TryGetGameFile(location.Path, out var gameFile) || !gameFile.TryCreateReader(out var reader))
                    continue;
                stream = reader;
            }
            else
            {
                stream = File.OpenRead(location.Path);
            }

            using (stream)
            {
                var candidateHash = Convert.ToHexString(GetAwbHash(stream, location.Path));
                _streamingAwbHashLookup[candidateHash] = location;
                if (candidateHash == hashString)
                {
                    return location;
                }
            }
        }

        return null;
    }

    private byte[] GetAwbHash(Stream stream, string awbName)
    {
        using var decryptedStream = CriWareAwbDecryption.CreateDecryptingStream(stream, awbName, _provider.Versions.Game, true);
        return MD5.HashData(decryptedStream ?? stream);
    }
}

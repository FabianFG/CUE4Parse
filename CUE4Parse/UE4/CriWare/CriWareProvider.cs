using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.CriWare.Decoders;
using CUE4Parse.UE4.CriWare.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;
using UE4Config.Parsing;

namespace CUE4Parse.UE4.CriWare;

public class CriWareExtractedSound
{
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => Name + "." + Extension.ToLowerInvariant();
}

public class CriWareProvider
{
    private readonly record struct AwbLocation(string Path, bool InProvider);
    private Dictionary<string, AwbLocation> _streamingAwbLookup = [];

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
        {
            CreateAwbLookupTable(_provider, awbDir);
        }

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
                    Log.Warning($"Skipping waveform extraction. Waveform encoding type '{wave.EncodeType}' is not supported");
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
                        Log.Warning($"Skipping waveform extraction. Waveform encoding type '{wave.EncodeType}' is not supported");
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
                Log.Warning($"Not all waveforms were extracted from ACB '{baseName}'. Extracted {visitedWaveforms.Count} out of {waveformsCount}.");
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
            var hashString = Convert.ToHexString(hash);
            if (!_streamingAwbLookup.TryGetValue(hashString, out var awbLocation))
                return null;

            Stream awbStream;
            if (awbLocation.InProvider)
            {
                if (!_provider.TryGetGameFile(awbLocation.Path, out var gameFile) ||
                    !gameFile.TryCreateReader(out var reader))
                    return null;

                awbStream = reader;
            }
            else
            {
                awbStream = File.OpenRead(awbLocation.Path);
            }

            awb = new AwbReader(awbStream);
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
            Log.Information($"CriWare content directory found at: {token.Value}");
        }
    }

    public void CreateAwbLookupTable(IFileProvider provider, string? overrideAwbDir = null)
    {
        if (_streamingAwbLookup.Count != 0)
            return;
        if (string.IsNullOrEmpty(_criWareContentDir) && string.IsNullOrEmpty(overrideAwbDir))
            return;

        var awbLookup = new Dictionary<string, AwbLocation>();

        var searchDirs = new List<string>(2);
        if (!string.IsNullOrEmpty(_criWareContentDir))
            searchDirs.Add(_criWareContentDir);
        if (!string.IsNullOrEmpty(overrideAwbDir))
            searchDirs.Add(overrideAwbDir);

        // From file system
        var awbFiles = Directory.EnumerateFiles(_gameDirectory, "*.awb", SearchOption.AllDirectories)
            .Where(f => searchDirs.Any(d => f.Replace('\\', '/').Contains(d)));

        foreach (var file in awbFiles)
        {
            using var stream = File.OpenRead(file);
            var hashBytes = MD5.HashData(stream);
            var hashString = Convert.ToHexString(hashBytes);

            awbLookup[hashString] = new AwbLocation(file, false);
        }

        // From provider
        var providerAwbFiles = provider.Files
            .Where(kv => kv.Key.EndsWith(".awb", StringComparison.OrdinalIgnoreCase)
                         && searchDirs.Any(d => kv.Key.Replace('\\', '/').Contains(d)));

        foreach (var (path, gameFile) in providerAwbFiles)
        {
            if (!gameFile.TryCreateReader(out var reader))
                continue;

            using (reader)
            {
                var hashBytes = MD5.HashData(reader);
                var hashString = Convert.ToHexString(hashBytes);

                awbLookup[hashString] = new AwbLocation(path, true);
            }
        }

        _streamingAwbLookup = awbLookup;
    }
}

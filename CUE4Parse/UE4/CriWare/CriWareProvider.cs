using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.CriWare.Decoders.HCA;
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

    private readonly struct CriWareAtomCues(int waveId, string name, bool isStreamed)
    {
        public int WaveId { get; } = waveId;
        public string Name { get; } = name;
        public bool IsStreamed { get; } = isStreamed;
    }

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

    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCueSheet cueSheet)
        => ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name, [], null);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AcbReader acb, string acbName)
        => ExtractCriWareSoundsInternal(acb, acbName, [], null);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AwbReader awb, string awbName)
        => ExtractFromAwb(awb, null, awbName, null, []);
    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomWaveBank awb)
    {
        if (awb?.AtomWaveBankData == null)
            return [];

        return ExtractFromAwb(awb.AtomWaveBankData, null, awb.Name, null, []);
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

        var soundCues = cueSheet.Properties.FirstOrDefault(p => p.Name.Text == "SoundCues");

        var atomCues = new List<CriWareAtomCues>();
        if (soundCues?.Tag is ArrayProperty array && array.Value != null)
        {
            for (int i = 0; i < array.Value.Properties.Count; i++)
            {
                var entry = array.Value.Properties[i];
                if (entry.GetValue<UAtomSoundCue>() is not { } atomCue)
                    continue;

                var waveInfo = atomCue.Properties
                    .FirstOrDefault(p => p.Name.Text == "WaveInfo")
                    ?.Tag?.GetValue<FStructFallback>();

                if (waveInfo?.TryGetValue(out int waveId, "WaveID") != true)
                    continue;

                var cueInfo = atomCue.Properties
                    .FirstOrDefault(p => p.Name.Text == "CueInfo")
                    ?.Tag?.GetValue<FStructFallback>();

                string cueName = $"{Path.GetFileNameWithoutExtension(cueSheet.Name)}_{i:D4}";

                cueInfo?.TryGetValue(out cueName, "Name");
                waveInfo.TryGetValue(out bool isStreamed, "bIsStreamed");

                atomCues.Add(new CriWareAtomCues(waveId, cueName, isStreamed));
            }
        }

        return ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name, atomCues, awb);
    }
    private List<CriWareExtractedSound> ExtractCriWareSoundsInternal(AcbReader? acb, string cueSheetName, List<CriWareAtomCues> atomCues, AwbReader? streamingAwb)
    {
        if (acb == null)
            return [];

        var memoryAwb = acb.GetAwb();
        streamingAwb ??= LoadStreamingAwb(acb);

        return ExtractFromAwb(memoryAwb, streamingAwb, cueSheetName, acb, atomCues);
    }

    private List<CriWareExtractedSound> ExtractFromAwb(AwbReader? memoryAwb, AwbReader? streamingAwb, string baseName, AcbReader? acb, List<CriWareAtomCues> atomCues)
    {
        var results = new List<CriWareExtractedSound>();
        var visitedWaveIds = new HashSet<Waveform>();

        if (acb != null)
        {
            var cueTable = acb.AtomCueSheetData["Cue"];
            var cueNameTable = acb.AtomCueSheetData["CueName"];

            foreach (var cueRow in cueTable)
            {
                int cueId = Convert.ToInt32(cueRow["CueId"]);
                int refId = Convert.ToInt32(cueRow["ReferenceIndex"]);
                var waveIds = acb.GetWaveIdFromCueId(cueId);
                var cueNameRow = cueNameTable.FirstOrDefault(cue => Convert.ToInt32(cue["CueIndex"]) == refId);
                var name = cueNameRow != null && cueNameRow["CueName"] is string cueName
                    ? cueName
                    : $"{Path.GetFileNameWithoutExtension(baseName)}_{refId:D4}";

                var index = 0;
                foreach (var wave in waveIds)
                {
                    if (!visitedWaveIds.Add(wave))
                        continue;

                    var hcaData = TryLoadHcaData(memoryAwb, streamingAwb, wave);

                    if (hcaData == null || hcaData.Length == 0)
                        continue;

                    results.Add(new CriWareExtractedSound
                    {
                        Name = waveIds.Count == 1 ? name : $"{name}_{index++:D4}",
                        Extension = "hca",
                        Data = hcaData
                    });
                }
            }
        }
        /*
        foreach (var atomCue in atomCues)
        {
            if (!visitedWaveIds.Add((atomCue.WaveId, atomCue.IsStreamed)))
                continue;

            var hcaData = TryLoadHcaData(memoryAwb, streamingAwb, atomCue.WaveId, atomCue.IsStreamed);

            if (hcaData == null || hcaData.Length == 0)
                continue;

            results.Add(new CriWareExtractedSound
            {
                Name = atomCue.Name,
                Extension = "hca",
                Data = hcaData
            });
        }

        for (int i = 0; i < memoryAwb?.Waves.Count; i++)
        {
            var wave = memoryAwb.Waves[i];

            if (!visitedWaveIds.Add((wave.WaveId, false)))
                continue;

            using var waveStream = memoryAwb.GetWaveSubfileStream(wave);
            if (waveStream.Length == 0)
                continue;

            string waveName = $"{Path.GetFileNameWithoutExtension(baseName)}_{wave.WaveId:D4}";

            var hcaData = HcaWaveStream.EmbedSubKey(waveStream, memoryAwb.Subkey);

            results.Add(new CriWareExtractedSound
            {
                Name = waveName,
                Extension = "hca",
                Data = hcaData
            });
        }

        for (int i = 0; i < streamingAwb?.Waves.Count; i++)
        {
            var wave = streamingAwb.Waves[i];

            if (!visitedWaveIds.Add((wave.WaveId, true)))
                continue;

            using var waveStream = streamingAwb.GetWaveSubfileStream(wave);
            if (waveStream.Length == 0)
                continue;

            string waveName = $"{Path.GetFileNameWithoutExtension(baseName)}_{wave.WaveId:D4}";

            var hcaData = HcaWaveStream.EmbedSubKey(waveStream, streamingAwb.Subkey);

            results.Add(new CriWareExtractedSound
            {
                Name = waveName,
                Extension = "hca",
                Data = hcaData
            });
        }
        */
        return results;
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
            var waveIds = acb.GetWaveIdFromCueId(cueId);
            if (waveIds.Count == 0)
                return results;

            var memoryAwb = acb.GetAwb();
            var streamingAwb = LoadStreamingAwb(acb);

            var index = 0;
            foreach (var wave in waveIds)
            {
                var hcaData = TryLoadHcaData(memoryAwb, streamingAwb, wave);

                if (hcaData == null || hcaData.Length == 0)
                    continue;

                results.Add(
                    new CriWareExtractedSound
                    {
                        Name = waveIds.Count == 1 ? cueName : $"{cueName}_{index++:D4}",
                        Extension = "hca",
                        Data = hcaData
                    }
                );
            }
        }

        return results;
    }

    private byte[]? TryLoadHcaData(AwbReader? awb, AwbReader? streamingAwb, Waveform waveform)
    {
        (AwbReader? reader, ushort waveId) = waveform.Streaming switch
        {
            WaveformStreamType.Memory  => (awb, waveform.Id),
            WaveformStreamType.Streaming or WaveformStreamType.Both => (streamingAwb,waveform.StreamId),
            _ => (null, 0),
        };
        if (reader == null) return null;

        var wave = reader.Waves.FirstOrDefault(w => w.WaveId == waveId);

        using var waveStream = reader.GetWaveSubfileStream(wave);
        return HcaWaveStream.EmbedSubKey(waveStream, reader.Subkey);
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

    public void CreateAwbLookupTable(IFileProvider provider)
    {
        var awbLookup = new Dictionary<string, AwbLocation>();

        if (string.IsNullOrEmpty(_criWareContentDir))
            return;

        // From file system
        var awbFiles = Directory.EnumerateFiles(_gameDirectory, "*.awb", SearchOption.AllDirectories)
            .Where(f => f.Replace('\\', '/').Contains(_criWareContentDir));

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
                         && kv.Key.Replace('\\', '/').Contains(_criWareContentDir));

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

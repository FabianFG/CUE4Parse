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

    private readonly struct CriWareAtomCues(int waveId, string name)
    {
        public int WaveId { get; } = waveId;
        public string Name { get; } = name;
    }

    public CriWareProvider(IFileProvider provider, string gameDirectory)
    {
        _provider = provider;
        _gameDirectory = gameDirectory;
        LoadCriWareConfig(provider);
        CreateAwbLookupTable(provider);
    }

    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCueSheet cueSheet)
        => ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name, []);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AcbReader acb, string acbName)
        => ExtractCriWareSoundsInternal(acb, acbName, []);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AwbReader awb, string awbName)
        => ExtractFromAwb(awb, awbName, null, []);
    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomWaveBank awb)
    {
        if (awb?.AtomWaveBankData == null)
            return [];

        return ExtractFromAwb(awb.AtomWaveBankData, awb.Name, null, []);
    }
    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomCueSheet cueSheet)
    {
        var soundCues = cueSheet.Properties.FirstOrDefault(p => p.Name.Text == "SoundCues");

        var atomCues = new List<CriWareAtomCues>();
        if (soundCues?.Tag is ArrayProperty array && array.Value != null)
        {
            foreach (var entry in array.Value.Properties)
            {
                var atomCue = entry.GetValue<UAtomSoundCue>();

                if (atomCue == null)
                    continue;

                var waveInfoProp = atomCue.Properties.FirstOrDefault(p => p.Name.Text == "WaveInfo");

                var waveInfoStruct = waveInfoProp?.Tag?.GetValue<FStructFallback>();
                if (waveInfoStruct?.TryGetValue(out int waveId, "WaveID") == null)
                    continue;

                var cueInfoProp = atomCue.Properties.FirstOrDefault(p => p.Name.Text == "CueInfo");

                var cueName = string.Empty;
                var cueInfoStruct = cueInfoProp?.Tag?.GetValue<FStructFallback>();
                cueInfoStruct?.TryGetValue(out cueName, "Name");

                atomCues.Add(new CriWareAtomCues(waveId, cueName ?? string.Empty));
            }
        }

        return ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name, atomCues);
    }
    private List<CriWareExtractedSound> ExtractCriWareSoundsInternal(AcbReader? acb, string cueSheetName, List<CriWareAtomCues> atomCues)
    {
        if (acb == null)
            return [];

        var awb = acb.GetAwb();
        var streamingAwb = LoadStreamingAwb(acb);

        if (streamingAwb != null)
            awb = streamingAwb;

        return awb == null ? [] : ExtractFromAwb(awb, cueSheetName, acb, atomCues);
    }

    private List<CriWareExtractedSound> ExtractFromAwb(AwbReader awb, string baseName, AcbReader? acb, List<CriWareAtomCues> atomCues)
    {
        var results = new List<CriWareExtractedSound>();
        var visitedWaveIds = new HashSet<int>();

        if (acb != null)
        {
            var cueTable = acb.AtomCueSheetData["Cue"];

            foreach (var cueRow in cueTable)
            {
                int cueId = Convert.ToInt32(cueRow["CueId"]);
                var waveId = acb.GetWaveIdFromCueId(cueId, acb.HasMemoryAwb);

                if (!visitedWaveIds.Add(waveId))
                    continue;

                var matchingWaves = awb.Waves.Where(w => w.WaveId == waveId).ToList();
                if (matchingWaves.Count == 0)
                    continue;

                foreach (var wave in matchingWaves)
                {
                    using var waveStream = awb.GetWaveSubfileStream(wave);
                    if (waveStream.Length == 0)
                        continue;

                    string waveName = acb.GetWaveName(waveId, 0, acb.HasMemoryAwb);
                    if (string.IsNullOrWhiteSpace(waveName))
                        waveName = $"{Path.GetFileNameWithoutExtension(baseName)}_{waveId:D4}";

                    var hcaData = HcaWaveStream.EmbedSubKey(waveStream, awb.Subkey);
                    results.Add(new CriWareExtractedSound
                    {
                        Name = waveName,
                        Extension = "hca",
                        Data = hcaData
                    });
                }
            }
        }

        foreach (var atomCue in atomCues)
        {
            if (!visitedWaveIds.Add(atomCue.WaveId))
                continue;

            var wave = awb.Waves.FirstOrDefault(w => w.WaveId == atomCue.WaveId);

            using var waveStream = awb.GetWaveSubfileStream(wave);
            if (waveStream.Length == 0)
                return [];

            var hcaData = HcaWaveStream.EmbedSubKey(waveStream, awb.Subkey);

            results.Add(new CriWareExtractedSound
            {
                Name = atomCue.Name,
                Extension = "hca",
                Data = hcaData
            });
        }

        for (int i = 0; i < awb.Waves.Count; i++)
        {
            var wave = awb.Waves[i];

            if (!visitedWaveIds.Add(wave.WaveId))
                continue;

            using var waveStream = awb.GetWaveSubfileStream(wave);
            if (waveStream.Length == 0)
                continue;

            string waveName = $"{Path.GetFileNameWithoutExtension(baseName)}_{wave.WaveId:D4}";

            var hcaData = HcaWaveStream.EmbedSubKey(waveStream, awb.Subkey);

            results.Add(new CriWareExtractedSound
            {
                Name = waveName,
                Extension = "hca",
                Data = hcaData
            });
        }

        return results;
    }

    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCue soundAtomCue)
    {
        var cueName = soundAtomCue.GetOrDefault<string>("CueName");
        var cueSheet = soundAtomCue.Properties.FirstOrDefault(p => p.Name.Text == "CueSheet");

        if (cueSheet?.Tag is ObjectProperty op && op.Value != null && op.Value.TryLoad<USoundAtomCueSheet>(out var atomCueSheet) && atomCueSheet != null)
        {
            var acb = atomCueSheet.AcbReader;

            if (acb == null)
                return [];

            var awb = acb.GetAwb();
            var streamingAwb = LoadStreamingAwb(acb);

            if (streamingAwb != null)
                awb = streamingAwb;

            if (awb == null)
                return [];

            var cueNameTable = acb.AtomCueSheetData["CueName"];
            var cueNameRow = cueNameTable.FirstOrDefault(cue =>
                string.Equals(cue["CueName"]?.ToString(), cueName, StringComparison.OrdinalIgnoreCase));

            if (cueNameRow == null)
                return [];

            int cueIndex = Convert.ToInt32(cueNameRow["CueIndex"]?.ToString());
            var cueRow = acb.AtomCueSheetData["Cue"][cueIndex];

            int cueId = Convert.ToInt32(cueRow["CueId"]);
            var waveId = acb.GetWaveIdFromCueId(cueId);

            var wave = awb.Waves.FirstOrDefault(w => w.WaveId == waveId);

            using var waveStream = awb.GetWaveSubfileStream(wave);
            if (waveStream.Length == 0)
                return [];

            var hcaData = HcaWaveStream.EmbedSubKey(waveStream, awb.Subkey);

            return [
                new CriWareExtractedSound
                {
                    Name = cueName,
                    Extension = "hca",
                    Data = hcaData
                }
            ];
        }

        return [];
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

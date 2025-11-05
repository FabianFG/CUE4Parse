using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.CriWare.Decoders.HCA;
using CUE4Parse.UE4.CriWare.Readers;
using NAudio.Wave;
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

    private IFileProvider _provider;
    private readonly string _gameDirectory;
    private string? _criWareContentDir;
    private ulong _decryptionKey;

    public CriWareProvider(IFileProvider provider, string gameDirectory, ulong key = 32105414741057402) // TODO: add criware key to settings
    {
        _provider = provider;
        _gameDirectory = gameDirectory;
        LoadCriWareConfig(provider);
        CreateAwbLookupTable(provider);
        _decryptionKey = key;
    }

    // TODO: grab hca wave by cue name provided with soundAtomCue
    //public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCue soundAtomCue)
    //{
    //    var cueName = soundAtomCue.GetOrDefault<string>("CueName");
    //    var cueSheet = soundAtomCue.Properties.FirstOrDefault(p => p.Name.Text == "CueSheet");

    //    if (cueSheet?.Tag is ObjectProperty op && op.Value.TryLoad(out USoundAtomCueSheet? atomCueSheet) && atomCueSheet != null)
    //    {
    //        var acb = atomCueSheet.AcbReader;
    //        var awb = acb?.GetAwb();
    //        if (awb == null || acb == null)
    //            return [];

    //        //int? waveId = acb.GetWaveIdFromWaveName(cueName);

    //        var results = new List<CriWareExtractedSound>(1);
    //        //if (waveId.HasValue)
    //        //{
    //        //    var waveEntry = awb.Waves.FirstOrDefault(w => w.WaveId == waveId.Value);
    //        //    var waveStream = awb.GetWaveSubfileStream(waveEntry);

    //        //    using var memoryStream = new MemoryStream();
    //        //    waveStream.CopyTo(memoryStream);
    //        //    byte[] waveData = memoryStream.ToArray();

    //        //    results.Add(new CriWareExtractedSound
    //        //    {
    //        //        Name = cueName,
    //        //        Extension = "hca", // can be ADX
    //        //        Data = waveData,
    //        //    });
    //        //}

    //        return results;
    //    }

    //    return [];
    //}

    public List<CriWareExtractedSound> ExtractCriWareSounds(UAtomCueSheet cueSheet)
        => ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name);
    public List<CriWareExtractedSound> ExtractCriWareSounds(USoundAtomCueSheet cueSheet)
        => ExtractCriWareSoundsInternal(cueSheet.AcbReader, cueSheet.Name);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AcbReader acb, string acbName)
        => ExtractCriWareSoundsInternal(acb, acbName);
    public List<CriWareExtractedSound> ExtractCriWareSounds(AwbReader awb, string awbName)
        => ExtractFromAwb(awb, awbName, null);
    private List<CriWareExtractedSound> ExtractCriWareSoundsInternal(AcbReader? acb, string cueSheetName)
    {
        if (acb == null)
            return [];

        AwbReader? awb = acb.GetAwb();

        var hash = acb.TryGetTableValue<byte[]>("StreamAwb", "Hash");
        if (hash != null)
        {
            var hashString = Convert.ToHexString(hash);
            if (!_streamingAwbLookup.TryGetValue(hashString, out var awbLocation))
                return [];

            Stream awbStream;
            if (awbLocation.InProvider)
            {
                if (!_provider.TryGetGameFile(awbLocation.Path, out var gameFile) ||
                    !gameFile.TryCreateReader(out var reader))
                    return [];

                awbStream = reader;
            }
            else
            {
                awbStream = File.OpenRead(awbLocation.Path);
            }

            awb = new AwbReader(awbStream);
            acb.HasMemoryAwb = false; // TODO
        }

        return awb == null ? [] : ExtractFromAwb(awb, cueSheetName, acb);
    }

    private List<CriWareExtractedSound> ExtractFromAwb(AwbReader awb, string baseName, AcbReader? acb)
    {
        var results = new List<CriWareExtractedSound>();
        for (int i = 0; i < awb.Waves.Count; i++)
        {
            var wave = awb.Waves[i];
            var waveStream = awb.GetWaveSubfileStream(wave);

            // TODO: how to handle if acb has memory awb and also has streaming awb?
            string waveName = $"{baseName}_{i:D4}";
            if (acb != null && acb.HasMemoryAwb)
                acb.GetWaveName(i, 0, acb.HasMemoryAwb);

            if (waveStream.Length == 0)
            {
                Log.Debug($"Skipping empty wave Name: {waveName}, WaveId: {wave.WaveId}");
                continue;
            }

            // TODO: for testing build audio as wav
            // change this later to output as hca and only decode to wav when audio is played
            // because this is going to be slow
            using var hcaWaveStream = new HcaWaveStream(waveStream, _decryptionKey, awb.Subkey);
            using var wavStream = new MemoryStream();
            using (var writer = new WaveFileWriter(wavStream, hcaWaveStream.WaveFormat))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = hcaWaveStream.Read(buffer, 0, buffer.Length)) > 0)
                    writer.Write(buffer, 0, bytesRead);
            }

            results.Add(new CriWareExtractedSound
            {
                Name = waveName,
                Extension = "wav",
                Data = wavStream.ToArray(),
            });
        }

        return results;
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

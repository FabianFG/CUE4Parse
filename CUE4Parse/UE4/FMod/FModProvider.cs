using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.FMod.Extensions;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using Fmod5Sharp.FmodTypes;
using Serilog;

namespace CUE4Parse.UE4.FMod;

public class FModExtractedSound
{
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => Name + "." + Extension.ToLowerInvariant();
}

public class FModProvider
{
    private Dictionary<FModGuid, List<FmodSample>> _resolvedEventsCache = [];
    private Dictionary<FModGuid, FModReader> _mergedReaders = [];

    public FModProvider(IFileProvider provider, string gameDirectory)
    {
        LoadPakBanks(provider);
        LoadFileBanks(gameDirectory);
        UpdateEventCache();
    }

    private void LoadPakBanks(IFileProvider provider)
    {
        var banks = provider.Files.Values
            .Where(x => x.Extension == "bank" && x.Path.Contains("FMOD", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Name.SubstringBefore('.'));

        foreach (var group in banks)
        {
            foreach (var file in group)
            {
                if (!provider.TrySaveAsset(file, out var data)) continue;
                if (!TryLoadBank(new MemoryStream(data), out var fmodBank))
                {
                    Log.Error("Failed to serialize FMOD Bank file {bank}", file);
                    continue;
                }

                if (_mergedReaders.TryGetValue(fmodBank.GetBankGuid(), out var merged))
                {
                    merged.Merge(fmodBank);
                }
                else
                {
                    _mergedReaders[fmodBank.GetBankGuid()] = fmodBank;
                }
            }
        }
    }

    public void LoadFileBanks(string gameDirectory)
    {
        var dir = new DirectoryInfo(gameDirectory);
        if (dir.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase) && Directory.GetParent(gameDirectory) is {} parentInfo)
            gameDirectory = parentInfo.FullName;

        string? fmodDir = Directory.GetDirectories(gameDirectory, "FMOD", SearchOption.AllDirectories)
            .SelectMany(fmodFolder => Directory.GetDirectories(fmodFolder, "Desktop", SearchOption.AllDirectories))
            .FirstOrDefault(Directory.Exists);

        if (fmodDir is null)
        {
            Log.Warning("FMOD Desktop directory not found under {0}", gameDirectory);
            return;
        }

        var banks = Directory.GetFiles(fmodDir, "*.bank", SearchOption.AllDirectories)
            .GroupBy(path => Path.GetFileName(path).SubstringBefore('.'));

        foreach (var group in banks)
        {
            foreach (var file in group)
            {
                if (!TryLoadBank(File.OpenRead(file), out var fmodBank))
                {
                    Log.Error("Failed to serialize FMOD Bank file {bank}", file);
                    continue;
                }

                if (_mergedReaders.TryGetValue(fmodBank.GetBankGuid(), out var merged))
                {
                    merged.Merge(fmodBank);
                }
                else
                {
                    _mergedReaders[fmodBank.GetBankGuid()] = fmodBank;
                }
            }
        }
    }

    public static bool TryLoadBank(Stream stream, [NotNullWhen(true)]out FModReader? fmodReader)
    {
        fmodReader = null;
        try
        {
            using var reader = new BinaryReader(stream);
            fmodReader = new FModReader(reader);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Can't load FMOD bank");
            return false;
        }
    }

    public void UpdateEventCache()
    {
        var eventSamples = new Dictionary<FModGuid, List<FmodSample>>();

        foreach (var fmodReader in _mergedReaders.Values)
        {
            var resolvedEvents = EventNodesResolver.ResolveAudioEvents(fmodReader);
#if DEBUG
            EventNodesResolver.LogMissingSamples(fmodReader, resolvedEvents);
#endif
            foreach (var kvp in resolvedEvents)
            {
                if (!eventSamples.TryGetValue(kvp.Key, out var sampleList))
                {
                    eventSamples[kvp.Key] = sampleList = [];
                }

                sampleList.AddRange(kvp.Value);
            }
        }

        _resolvedEventsCache = eventSamples;
    }

    public List<FModExtractedSound> ExtractEventSounds(UFMODEvent audioEvent)
    {
        if (!audioEvent.TryGet<FGuid>("AssetGuid", out var fguid)) return [];
        var eventGuid = new FModGuid(fguid);

        if (!_resolvedEventsCache.TryGetValue(eventGuid, out var samples))
        {
            Log.Warning("Can't find FMODEvent with the guid {0}", eventGuid);
            return [];
        }

        var eventName = audioEvent.Name;
        var extracted = new List<FModExtractedSound>(samples.Count);
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            if (!sample.RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension))
                continue;

            extracted.Add(new FModExtractedSound
            {
                Name = sample.Name ?? $"{eventName}_{i}",
                Extension = fileExtension,
                Data = dataBytes
            });
        }

        return extracted;
    }

    public List<FModExtractedSound> ExtractBankSounds(UFMODBank audioBank)
    {
        var assetGuid = audioBank.GetOrDefault<FGuid>("AssetGuid");
        var bankGuid = new FModGuid(assetGuid);

        if (!_mergedReaders.TryGetValue(bankGuid, out var bank))
        {
            Log.Warning("Can't find FMODBank with the guid {0}", bankGuid);
            return [];
        }

        var bankName = audioBank.Name;
        var samples = bank.ExtractTracks();
        var extracted = new List<FModExtractedSound>();
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            if (!sample.RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension))
                continue;

            extracted.Add(new FModExtractedSound
            {
                Name = sample.Name ?? $"{bankName}_{i}",
                Extension = fileExtension,
                Data = dataBytes
            });
        }

        return extracted;
    }
}

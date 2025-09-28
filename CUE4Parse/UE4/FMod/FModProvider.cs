using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.FMod.Extensions;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using Fmod5Sharp.FmodTypes;

namespace CUE4Parse.UE4.FMod;

public class FModExtractedSound
{
    public required string OutputPath { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => OutputPath + "." + Extension.ToLowerInvariant();
}

public static class FModProvider
{
    private static Dictionary<FModGuid, List<FmodSample>>? _resolvedEventsCache;

    public static FModExtractedSound[] ExtractBankSounds(UFMODEvent audioEvent, string gameDirectory)
    {
        var eventName = audioEvent.Name;

        var assetGuidProp = audioEvent.Properties.FirstOrDefault(p => p.Name.Text == "AssetGuid")
            ?? throw new InvalidOperationException($"Event {audioEvent.Name} has no AssetGuid");

        var fguid = assetGuidProp.Tag?.GetValue<FGuid>() ?? throw new InvalidOperationException();
        var eventGuid = new FModGuid(fguid);

        string fmodDir = Directory
            .GetDirectories(gameDirectory, "FMOD", SearchOption.AllDirectories)
            .Select(dir => Path.Combine(dir, "Desktop"))
            .FirstOrDefault(Directory.Exists)
            ?? throw new DirectoryNotFoundException($"FMOD Desktop directory not found under {gameDirectory}");

        if (_resolvedEventsCache == null)
        {
            var mergedReaders = FModBankMerger.MergeBanks(fmodDir);
            var eventSamples = new Dictionary<FModGuid, List<FmodSample>>();

            foreach (var fmodReader in mergedReaders)
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

        if (!_resolvedEventsCache.TryGetValue(eventGuid, out var samples))
            return [];

        var extracted = new List<FModExtractedSound>(samples.Count);
        foreach (var sample in samples)
        {
            if (!sample.RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension) ||
                dataBytes == null || fileExtension == null)
                continue;

            extracted.Add(new FModExtractedSound
            {
                OutputPath = Path.Combine(fmodDir, sample.Name ?? eventName),
                Extension = fileExtension,
                Data = dataBytes
            });
        }

        return [.. extracted];
    }
}

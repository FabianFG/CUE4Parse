
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.FMod.Nodes;
using CUE4Parse.UE4.FMod.Objects;
using Fmod5Sharp.FmodTypes;
using Serilog;

namespace CUE4Parse.UE4.FMod.Extensions;

public static class EventNodesResolver
{
    public static Dictionary<FModGuid, List<FmodSample>> ResolveAudioEvents(FModReader reader)
    {
        var result = new Dictionary<FModGuid, List<FmodSample>>();
        foreach (var (eventGuid, evNode) in reader.EventNodes)
        {
            var samples = ResolveEventNodesWithAudio(reader, evNode);
            if (samples.Count > 0)
                result[eventGuid] = samples;
        }
        return result;
    }

    private static List<FmodSample> ResolveEventNodesWithAudio(FModReader reader, EventNode evNode)
    {
        var result = new HashSet<FmodSample>();
        var visited = new HashSet<FModGuid>();
        var stack = new Stack<FModGuid>();

        if (reader.TimelineNodes.TryGetValue(evNode.TimelineGuid, out var tmlNode))
        {
            PushTriggerBoxes(stack, tmlNode.TriggerBoxes);
            PushTriggerBoxes(stack, tmlNode.TimeLockedTriggerBoxes);
        }
        else
        {
            stack.Push(evNode.TimelineGuid);
        }

        if (evNode.ParameterLayouts != null)
        {
            foreach (var param in evNode.ParameterLayouts)
                stack.Push(param);

            foreach (var paramGuid in evNode.ParameterLayouts)
            {
                if (reader.ParameterLayoutNodes != null && reader.ParameterLayoutNodes.TryGetValue(paramGuid, out var paramLayoutNode))
                {
                    if (paramLayoutNode.Instruments != null)
                        foreach (var instGuid in paramLayoutNode.Instruments)
                            stack.Push(instGuid);

                    if (paramLayoutNode.Controllers != null)
                        foreach (var controllerGuid in paramLayoutNode.Controllers)
                            stack.Push(controllerGuid);

                    if (paramLayoutNode.TriggerBoxes != null)
                        foreach (var triggerBoxGuid in paramLayoutNode.TriggerBoxes)
                            stack.Push(triggerBoxGuid);
                }
            }
        }

        if (evNode.EventTriggeredInstruments != null)
            foreach (var inst in evNode.EventTriggeredInstruments)
                stack.Push(inst);

        while (stack.Count > 0)
        {
            var guid = stack.Pop();
            if (!visited.Add(guid))
                continue;

            if (reader.PlaylistNodes.TryGetValue(guid, out var plstNode))
            {
                foreach (var plstEntry in plstNode.Entries)
                    stack.Push(plstEntry.Guid);

                continue;
            }

            if (reader.WaveformInstrumentNodes.TryGetValue(guid, out var wavGuid) &&
                reader.WavEntries.TryGetValue(wavGuid, out var entry) &&
                entry.SoundBankIndex < reader.SoundBankData[entry.SubsoundIndex].Samples.Count)
            {
                result.Add(reader.SoundBankData[entry.SubsoundIndex].Samples[entry.SoundBankIndex]);
                continue;
            }

            if (reader.TimelineNodes.TryGetValue(guid, out var tmlNode2))
            {
                PushTriggerBoxes(stack, tmlNode2.TriggerBoxes);
                PushTriggerBoxes(stack, tmlNode2.TimeLockedTriggerBoxes);
            }
        }

        return [.. result];
    }

    private static void PushTriggerBoxes(Stack<FModGuid> stack, FTriggerBox[] boxes)
    {
        foreach (var box in boxes)
            stack.Push(box.Guid);
    }

    private static HashSet<string> GetAllResolvedSampleNames(Dictionary<FModGuid, List<FmodSample>> resolvedEvents)
    {
        var allResolvedNames = new HashSet<string>();

        foreach (var samples in resolvedEvents.Values)
            foreach (var sample in samples)
                allResolvedNames.Add(sample.Name!);

        return allResolvedNames;
    }

    public static Dictionary<FModGuid, FmodSample> GetUnreferencedSamplesWithGuids(FModReader reader, HashSet<string> allResolved)
    {
        var unreferenced = new Dictionary<FModGuid, FmodSample>();

        foreach (var kvp in reader.WavEntries)
        {
            var wavGuid = kvp.Key;
            var entry = kvp.Value;

            if (entry.SoundBankIndex >= reader.SoundBankData[entry.SubsoundIndex].Samples.Count)
                continue;

            var sample = reader.SoundBankData[entry.SubsoundIndex].Samples[entry.SoundBankIndex];

            if (!allResolved.Contains(sample.Name!))
            {
                unreferenced[wavGuid] = sample;
            }
        }

        return unreferenced;
    }

    public static void LogMissingSamples(FModReader reader, Dictionary<FModGuid, List<FmodSample>> resolvedEvents)
    {
        Log.Debug($"----------------");
        int sampleCount = resolvedEvents.Values.Sum(samples => samples?.Count ?? 0);

        Log.Debug($"+ Resolved {sampleCount} audio sample(s)");

        var allResolved = GetAllResolvedSampleNames(resolvedEvents);
        var unreferencedSamples = GetUnreferencedSamplesWithGuids(reader, allResolved);

        if (unreferencedSamples.Count == 0)
        {
            Log.Debug("All audio samples were resolved");
            return;
        }

        Log.Debug($"- Unresolved {unreferencedSamples.Count} audio sample(s):");
        foreach (var sample in unreferencedSamples)
        {
            Log.Debug($"'{sample.Value.Name}' sample wasn't resolved (GUID: {sample.Key})");
        }
    }
}

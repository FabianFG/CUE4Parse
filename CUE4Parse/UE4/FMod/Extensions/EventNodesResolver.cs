using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.FMod.Nodes;
using CUE4Parse.UE4.FMod.Nodes.Instruments;
using CUE4Parse.UE4.FMod.Nodes.Transitions;
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
            foreach (var box in tmlNode.TriggerBoxes)
                stack.Push(box.Guid);
            foreach (var box in tmlNode.TimeLockedTriggerBoxes)
                stack.Push(box.Guid);
            foreach (var namedMarker in tmlNode.TimelineNamedMarkers)
                stack.Push(namedMarker.BaseGuid);
            foreach (var tempoMarker in tmlNode.TimelineTempoMarkers)
                stack.Push(tempoMarker.BaseGuid);
        }
        else
        {
            stack.Push(evNode.TimelineGuid);
        }

        foreach (var paramGuid in evNode.ParameterLayouts)
        {
            stack.Push(paramGuid);
            if (reader.ParameterLayoutNodes.TryGetValue(paramGuid, out var paramLayoutNode))
            {
                foreach (var instGuid in paramLayoutNode.Instruments)
                    stack.Push(instGuid);
                foreach (var triggerBoxGuid in paramLayoutNode.TriggerBoxes)
                    stack.Push(triggerBoxGuid.InstrumentGuid);
            }
        }

        foreach (var inst in evNode.EventTriggeredInstruments)
            stack.Push(inst);

        var destinationLookup = reader.TransitionNodes.Values
            .OfType<TransitionRegionNode>()
            .ToLookup(n => n.DestinationGuid);

        while (stack.Count > 0)
        {
            var guid = stack.Pop();
            if (!visited.Add(guid))
                continue;

            if (reader.TimelineNodes.TryGetValue(guid, out var tmlNode2))
            {
                foreach (var box in tmlNode2.TriggerBoxes)
                    stack.Push(box.Guid);
                foreach (var box in tmlNode2.TimeLockedTriggerBoxes)
                    stack.Push(box.Guid);
                foreach (var namedMarker in tmlNode2.TimelineNamedMarkers)
                    stack.Push(namedMarker.BaseGuid);
                foreach (var tempoMarker in tmlNode2.TimelineTempoMarkers)
                    stack.Push(tempoMarker.BaseGuid);
            }

            foreach (var transTimeline in destinationLookup[guid])
            {
                if (transTimeline.TransitionBody != null)
                {
                    foreach (var fade in transTimeline.TransitionBody.FadeOverrides)
                    {
                        stack.Push(fade.CurveGuid);
                        stack.Push(fade.ControllerGuid);
                    }

                    foreach (var box in transTimeline.TransitionBody.TriggeredTriggerBoxes)
                        stack.Push(box.Guid);

                    foreach (var box in transTimeline.TransitionBody.TimeLockedTriggerBoxes)
                        stack.Push(box.Guid);
                }
            }

            if (reader.InstrumentNodes.TryGetValue(guid, out var baseInstrNode))
            {
                if (baseInstrNode.InstrumentBody != null)
                {
                    stack.Push(baseInstrNode.InstrumentBody.TimelineGuid);
                }

                if (baseInstrNode is MultiInstrumentNode multiInst && multiInst.PlaylistBody != null)
                {
                    foreach (var plEntry in multiInst.PlaylistBody.Entries)
                        stack.Push(plEntry.Guid);
                }
                else if (baseInstrNode is ScattererInstrumentNode scatterInst && scatterInst.PlaylistBody != null)
                {
                    foreach (var plEntry in scatterInst.PlaylistBody.Entries)
                        stack.Push(plEntry.Guid);
                }

                if (baseInstrNode is WaveformInstrumentNode wavInstr)
                {
                    if (reader.WavEntries.TryGetValue(wavInstr.WaveformResourceGuid, out var entry) &&
                        reader.SoundBankData.Count > 0 &&
                        entry.SubsoundIndex < reader.SoundBankData.Count &&
                        entry.SoundBankIndex < reader.SoundBankData[entry.SubsoundIndex].Samples.Count)
                    {
                        result.Add(reader.SoundBankData[entry.SubsoundIndex].Samples[entry.SoundBankIndex]);
                    }
                }
            }
        }

        return [.. result];
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

            if (reader.SoundBankData.Count <= 0 || entry.SoundBankIndex >= reader.SoundBankData[entry.SubsoundIndex].Samples.Count)
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
        Log.Debug("----------------");
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

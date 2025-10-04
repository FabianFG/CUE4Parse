using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CUE4Parse.UE4.FMod.Extensions;

public static class FModBankMerger
{
    // Events and samples can be divided between streams/assets/bank
    public static List<FModReader> MergeBanks(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.bank", SearchOption.TopDirectoryOnly);
        var baseNames = files
            .Select(f => Path.GetFileName(f)
                .Replace(".streams.bank", "")
                .Replace(".assets.bank", "")
                .Replace(".bank", ""))
            .Distinct();

        var mergedReaders = new List<FModReader>();
        foreach (var baseName in baseNames)
        {
            var variants = new[]
            {
                Path.Combine(folderPath, baseName + ".bank"),
                Path.Combine(folderPath, baseName + ".assets.bank"),
                Path.Combine(folderPath, baseName + ".streams.bank"),
            };

            FModReader? merged = null;
            foreach (var file in variants.Where(File.Exists))
            {
                using var reader = new BinaryReader(File.OpenRead(file));
                var fmod = new FModReader(reader);
                if (merged == null)
                {
                    merged = fmod;
                }
                else
                {
                    MergeInto(merged, fmod);
                }
            }

            if (merged != null)
                mergedReaders.Add(merged);
        }

        return mergedReaders;
    }

    private static void MergeInto(FModReader dest, FModReader src)
    {
        foreach (var kv in src.EventNodes)
            dest.EventNodes[kv.Key] = kv.Value;
        foreach (var kv in src.TimelineNodes)
            dest.TimelineNodes[kv.Key] = kv.Value;
        foreach (var kv in src.PlaylistNodes)
            dest.PlaylistNodes[kv.Key] = kv.Value;
        foreach (var kv in src.InstrumentNodes)
            dest.InstrumentNodes[kv.Key] = kv.Value;
        foreach (var kv in src.WavEntries)
            dest.WavEntries[kv.Key] = kv.Value;
        foreach (var kv in src.ScattererInstrumentNodes)
            dest.ScattererInstrumentNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ParameterNodes)
            dest.ParameterNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ModulatorNodes)
            dest.ModulatorNodes[kv.Key] = kv.Value;
        foreach (var kv in src.CurveNodes)
            dest.CurveNodes[kv.Key] = kv.Value;
        foreach (var kv in src.PropertyNodes)
            dest.PropertyNodes[kv.Key] = kv.Value;
        foreach (var kv in src.MappingNodes)
            dest.MappingNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ParameterLayoutNodes)
            dest.ParameterLayoutNodes[kv.Key] = kv.Value;
        foreach (var kv in src.WaveformInstrumentNodes)
            dest.WaveformInstrumentNodes[kv.Key] = kv.Value;
        foreach (var kv in src.ControllerNodes)
            dest.ControllerNodes[kv.Key] = kv.Value;

        dest.SoundBankData.AddRange(src.SoundBankData);
    }
}

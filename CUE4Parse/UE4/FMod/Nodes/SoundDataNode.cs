using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Serilog;
using System;
using System.IO;

namespace CUE4Parse.UE4.FMod.Nodes;

public class SoundDataNode
{
    public readonly FmodSoundBank? SoundBank;

    public SoundDataNode(BinaryReader Ar, long nodeStart, int size, int soundDataIndex)
    {
        byte[] sndChunk = Ar.ReadBytes(size);

        uint fsbOffset = FModReader.SoundDataInfo!.Header[soundDataIndex].FSBOffset;

        var relativeOffset = (int)(fsbOffset - nodeStart) - 8;

        byte[] fsbBytes = sndChunk[relativeOffset..];

        // TODO: Fmod5Sharp doesn't support more than 2 audio channels
        // we can either fix this by falling back to vgmstream which does or try to fix it in Fmod5Sharp
        try
        {
            if (FsbLoader.TryLoadFsbFromByteArray(fsbBytes, out var bank) && bank != null)
            {
                SoundBank = bank;
#if DEBUG
                Log.Debug($"FSB5 parsed successfully, samples: {bank.Samples.Count}");
#endif
                for (int i = 0; i < bank.Samples.Count; i++)
                {
                    var sample = bank.Samples[i];
#if DEBUG
                    Log.Debug($"Sample: {sample.Name}, Index: {i}");
#endif
                }
            }
            else
            {
                Log.Error($"Failed to parse FSB5 at {fsbOffset}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception thrown while parsing FSB5 at {fsbOffset}: {ex.Message}");
        }
    }
}

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

        // In case FSB5 is encrypted
        if (!Fsb5Decryption.IsFSB5Header(fsbBytes))
        {
#if DEBUG
            Log.Debug($"Found encrypted FSB5 header at {fsbOffset}");
#endif

            if (FModReader.EncryptionKey == null)
                throw new Exception("FSB5 is encrypted, but encryption key wasn't provided, cannot decrypt");

            Fsb5Decryption.Decrypt(fsbBytes, FModReader.EncryptionKey);
        }

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

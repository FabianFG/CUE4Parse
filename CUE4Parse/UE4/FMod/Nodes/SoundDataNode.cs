using System;
using System.IO;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;
using Serilog;
using SubstreamSharp;

namespace CUE4Parse.UE4.FMod.Nodes;

public class SoundDataNode
{
    public readonly FmodSoundBank? SoundBank;

    public SoundDataNode(BinaryReader Ar, long nodeStart, uint size, int soundDataIndex)
    {
        uint fsbOffset = FModReader.SoundDataInfo!.Header[soundDataIndex].FSBOffset;
        var relativeOffset = fsbOffset - nodeStart - 8;
        Ar.BaseStream.Position = fsbOffset;
        Stream fsbStream = Ar.BaseStream.Substream(fsbOffset, size);

        // In case FSB5 is encrypted
        if (!Fsb5Decryption.IsFSB5Header(Ar.BaseStream))
        {
#if DEBUG
            Log.Debug($"Encrypted FSB5 header at {fsbOffset}");
#endif
            fsbStream = Fsb5Decryption.Decrypt(fsbStream, FModReader.EncryptionKey);
        }

        try
        {
            if (FsbLoader.TryLoadFsbFromStream(fsbStream, out var bank) && bank != null)
            {
                SoundBank = bank;
                Ar.BaseStream.Position = fsbOffset - relativeOffset + size;
#if DEBUG
                Log.Debug($"FSB5 parsed successfully, samples: {bank.Samples.Count}");
#endif
                var audioType = bank.Header.AudioType;
                if (!audioType.IsSupported())
                    Log.Error($"Soundbank uses unsupported audio format: {audioType}");
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

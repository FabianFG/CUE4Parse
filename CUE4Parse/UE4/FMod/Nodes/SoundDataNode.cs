using System;
using System.IO;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;

namespace CUE4Parse.UE4.FMod.Nodes;

public class SoundDataNode
{
    //private static readonly byte[] FSB5 = Encoding.ASCII.GetBytes("FSB5");

    public FmodSoundBank? SoundBank { get; }

    public SoundDataNode(BinaryReader Ar, long nodeStart, int size, int soundDataIndex)
    {
        byte[] sndChunk = Ar.ReadBytes(size);

        //int fsbPos = FindFsbOffset(sndChunk);
        //if (fsbPos < 0)
        //{
        //    Console.WriteLine("No FSB5 found in SND chunk");
        //    return;
        //}

        //Console.WriteLine($"Found FSB5 at offset {fsbPos} inside SND chunk");

        int fsbOffset = FModReader.SoundDataHeader!.Header[soundDataIndex].FSBOffset;

        var relativeOffset = (int) (fsbOffset - nodeStart) - 8;

        byte[] fsbBytes = sndChunk[relativeOffset..];

        if (FsbLoader.TryLoadFsbFromByteArray(fsbBytes, out var bank) && bank != null)
        {
            SoundBank = bank;
            Console.WriteLine($"FSB5 parsed successfully, samples: {bank.Samples.Count}");
            for (int i = 0; i < bank.Samples.Count; i++)
            {
                var sample = bank.Samples[i];
                Console.WriteLine($"Sample: {sample.Name}, Index: {i}");
            }
        }
        else
        {
            Console.WriteLine("Failed to parse FSB5 at detected position");
        }
    }

    //private static int FindFsbOffset(byte[] data)
    //{
    //    int maxScan = Math.Min(data.Length, 64);

    //    for (int i = 0; i <= maxScan - FSB5.Length; i++)
    //    {
    //        if (data[i..(i + FSB5.Length)].SequenceEqual(FSB5))
    //            return i;
    //    }

    //    return -1;
    //}
}

using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicRanSeqPlaylistItem
{
    public readonly uint SegmentId;
    public readonly uint PlaylistItemId;
    public readonly uint NumChildren;
    public readonly List<AkMusicRanSeqPlaylistItem> Children;
    public readonly LoopInfo LoopInfo;
    public readonly WeightInfo WeightInfo;

    public AkMusicRanSeqPlaylistItem(FArchive Ar)
    {
        SegmentId = Ar.Read<uint>();
        PlaylistItemId = Ar.Read<uint>();
        NumChildren = Ar.Read<uint>();
        Children = [];

        if (WwiseVersions.Version <= 36)
        {
            if (NumChildren != 0)
            {
                LoopInfo = new LoopInfo(Ar);
                WeightInfo = new WeightInfo(Ar);
            }
            else
            {
                SegmentId = Ar.Read<uint>();
                LoopInfo = new LoopInfo(Ar);
                WeightInfo = new WeightInfo(Ar);
            }
        }
        else
        {
            if (WwiseVersions.Version <= 44)
            {
                if (NumChildren == 0)
                {
                    SegmentId = Ar.Read<uint>();
                }
                else
                {
                    Ar.Read<uint>(); // eRSType
                }
            }
            else
            {
                Ar.Read<uint>(); // eRSType
            }

            LoopInfo = new LoopInfo(Ar);
            WeightInfo = new WeightInfo(Ar);
        }

        for (int i = 0; i < NumChildren; i++)
        {
            var child = new AkMusicRanSeqPlaylistItem(Ar);
            Children.Add(child);
        }
    }
}

public class LoopInfo
{
    public readonly short Loop;
    public readonly short? LoopMin;
    public readonly short? LoopMax;

    public LoopInfo(FArchive Ar)
    {
        Loop = Ar.Read<short>();

        if (WwiseVersions.Version > 89)
        {
            LoopMin = Ar.Read<short>();
            LoopMax = Ar.Read<short>();
        }
    }
}

public class WeightInfo
{
    public readonly ushort Weight;
    public readonly ushort? AvoidRepeatCount;
    public readonly byte IsUsingWeight;
    public readonly byte IsShuffle;

    public WeightInfo(FArchive Ar)
    {
        if (WwiseVersions.Version <= 56)
        {
            Weight = Ar.Read<ushort>();
        }
        else
        {
            Weight = (ushort) Ar.Read<uint>();
        }

        AvoidRepeatCount = Ar.Read<ushort>();
        IsUsingWeight = Ar.Read<byte>();
        IsShuffle = Ar.Read<byte>();
    }
}

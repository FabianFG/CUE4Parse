using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicRanSeqPlaylistItem
{
    public uint SegmentId { get; private set; }
    public uint PlaylistItemId { get; private set; }
    public uint NumChildren { get; private set; }
    public List<AkMusicRanSeqPlaylistItem> Children { get; private set; }
    public LoopInfo LoopInfo { get; private set; }
    public WeightInfo WeightInfo { get; private set; }

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
    public short Loop { get; private set; }
    public short? LoopMin { get; private set; }
    public short? LoopMax { get; private set; }

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
    public ushort Weight { get; private set; }
    public ushort? AvoidRepeatCount { get; private set; }
    public byte IsUsingWeight { get; private set; }
    public byte IsShuffle { get; private set; }

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

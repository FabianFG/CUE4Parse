using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTrackSrcInfo
{
    public uint TrackID { get; private set; }
    public ulong SourceID { get; private set; }
    public uint CacheID { get; private set; }
    public ulong EventID { get; private set; }
    public double PlayAt { get; private set; }
    public double BeginTrimOffset { get; private set; }
    public double EndTrimOffset { get; private set; }
    public double SrcDuration { get; private set; }

    public AkTrackSrcInfo(FArchive Ar)
    {
        TrackID = Ar.Read<uint>();
        SourceID = Ar.Read<ulong>();
        if (WwiseVersions.WwiseVersion > 150)
        {
            CacheID = Ar.Read<uint>();
        }

        if (WwiseVersions.WwiseVersion > 132)
        {
            EventID = Ar.Read<ulong>();
        }

        PlayAt = Ar.Read<double>();
        BeginTrimOffset = Ar.Read<double>();
        EndTrimOffset = Ar.Read<double>();
        SrcDuration = Ar.Read<double>();
    }
}

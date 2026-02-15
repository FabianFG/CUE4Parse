using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkTrackSrcInfo
{
    public readonly uint TrackId;
    public readonly uint SourceId;
    public readonly uint CacheId;
    public readonly uint EventId;
    public readonly double PlayAt;
    public readonly double BeginTrimOffset;
    public readonly double EndTrimOffset;
    public readonly double SrcDuration;

    public AkTrackSrcInfo(FArchive Ar)
    {
        TrackId = Ar.Read<uint>();
        SourceId = Ar.Read<uint>();

        if (WwiseVersions.Version > 150)
        {
            CacheId = Ar.Read<uint>();
        }

        if (WwiseVersions.Version > 132)
        {
            EventId = Ar.Read<uint>();
        }

        PlayAt = Ar.Read<double>();
        BeginTrimOffset = Ar.Read<double>();
        EndTrimOffset = Ar.Read<double>();
        SrcDuration = Ar.Read<double>();
    }
}

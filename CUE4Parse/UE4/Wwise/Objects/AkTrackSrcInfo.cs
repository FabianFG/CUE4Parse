using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTrackSrcInfo
{
    public uint TrackId { get; private set; }
    public uint SourceId { get; private set; }
    public uint CacheId { get; private set; }
    public uint EventId { get; private set; }
    public double PlayAt { get; private set; }
    public double BeginTrimOffset { get; private set; }
    public double EndTrimOffset { get; private set; }
    public double SrcDuration { get; private set; }

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

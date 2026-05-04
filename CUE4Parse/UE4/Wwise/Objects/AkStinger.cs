using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStinger
{
    public readonly uint TriggerId;
    public readonly uint SegmentId;
    public readonly EAkSyncType SyncPlayAt;
    public readonly uint CueFilterHash;
    public readonly int DontRepeatTime;
    public readonly uint NumSegmentLookAhead;

    public AkStinger(FWwiseArchive Ar)
    {
        TriggerId = Ar.Read<uint>();
        SegmentId = Ar.Read<uint>();
        SyncPlayAt = Ar.Read<EAkSyncType>();

        if (Ar.Version > 62)
        {
            CueFilterHash = Ar.Read<uint>();
        }

        DontRepeatTime = Ar.Read<int>();
        NumSegmentLookAhead = Ar.Read<uint>();
    }

    public static AkStinger[] ReadArray(FWwiseArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkStinger(Ar));
}

using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStinger
{
    public readonly uint TriggerId;
    public readonly uint SegmentId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ESyncType SyncPlayAt;
    public readonly uint CueFilterHash;
    public readonly int DontRepeatTime;
    public readonly uint NumSegmentLookAhead;

    public AkStinger(FArchive Ar)
    {
        TriggerId = Ar.Read<uint>();
        SegmentId = Ar.Read<uint>();
        SyncPlayAt = Ar.Read<ESyncType>();

        if (WwiseVersions.Version > 62)
        {
            CueFilterHash = Ar.Read<uint>();
        }

        DontRepeatTime = Ar.Read<int>();
        NumSegmentLookAhead = Ar.Read<uint>();
    }

    public static AkStinger[] ReadArray(FArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkStinger(Ar));
}

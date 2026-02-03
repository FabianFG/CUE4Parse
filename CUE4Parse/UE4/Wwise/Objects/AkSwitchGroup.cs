using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkSwitchGroup
{
    public readonly uint SwitchGroupId;
    public readonly uint RtpcId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkGameSyncType RtpcType;
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkSwitchGroup(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        RtpcId = Ar.Read<uint>();
        if (WwiseVersions.Version > 89)
            RtpcType = Ar.Read<EAkGameSyncType>();
        GraphPoints = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRtpcGraphPoint(Ar));
    }
}

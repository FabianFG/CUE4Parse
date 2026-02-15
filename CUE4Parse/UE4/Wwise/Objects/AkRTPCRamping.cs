using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkRTPCRamping
{
    public readonly uint RtpcId;
    public readonly float Value;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkTransitionRampingType RampType;
    public readonly float RampUp;
    public readonly float RampDown;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkBuiltInParam BindToBuiltInParam;

    public AkRTPCRamping(FArchive Ar)
    {
        RtpcId = Ar.Read<uint>();
        Value = Ar.Read<float>();

        if (WwiseVersions.Version > 89)
        {
            RampType = Ar.Read<EAkTransitionRampingType>();
            RampUp = Ar.Read<float>();
            RampDown = Ar.Read<float>();
            // CAkGameSyncMgr::BindGameSyncToBuiltIn
            BindToBuiltInParam = Ar.Read<EAkBuiltInParam>();
        }
    }
}

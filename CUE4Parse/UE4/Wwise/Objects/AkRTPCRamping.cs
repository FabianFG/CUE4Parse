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

    public AkRTPCRamping(FWwiseArchive Ar)
    {
        RtpcId = Ar.Read<uint>();
        Value = Ar.Read<float>();

        if (Ar.Version > 89)
        {
            RampType = Ar.Read<EAkTransitionRampingType>();
            RampUp = Ar.Read<float>();
            RampDown = Ar.Read<float>();
            // CAkGameSyncMgr::BindGameSyncToBuiltIn
            BindToBuiltInParam = Ar.Read<EAkBuiltInParam>();
        }
    }
}

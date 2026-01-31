using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkRtpcGraphPoint
{
    public readonly float From;
    public readonly float To;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkCurveInterpolation Interpolation;

    public AkRtpcGraphPoint(FArchive Ar)
    {
        From = Ar.Read<float>();
        To = Ar.Read<float>();
        Interpolation = (EAkCurveInterpolation) Ar.Read<uint>();
    }

    public static AkRtpcGraphPoint[] ReadArray(FArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRtpcGraphPoint(Ar));
}

public readonly struct AkRtpc
{
    public readonly uint RtpcId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkGameSyncType RtpcType;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkRtpcAccum RtpcAccum;
    public readonly int ParamId;
    public readonly uint RtpcCurveId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkCurveScaling Scaling;
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkRtpc(FArchive Ar)
    {
        RtpcId = Ar.Read<uint>();

        if (WwiseVersions.Version > 89)
        {
            RtpcType = Ar.Read<EAkGameSyncType>();
            RtpcAccum = Ar.Read<EAkRtpcAccum>();
        }

        ParamId = WwiseVersions.Version switch
        {
            <= 89 => Ar.Read<int>(),
            <= 113 => Ar.Read<byte>(),
            _ => Ar.Read7BitEncodedInt()
        };

        RtpcCurveId = Ar.Read<uint>();
        Scaling = Ar.Read<EAkCurveScaling>();
        GraphPoints = Ar.ReadArray(Ar.Read<ushort>(), () => new AkRtpcGraphPoint(Ar));
    }

    public static AkRtpc[] ReadArray(FArchive Ar) =>
        Ar.ReadArray(Ar.Read<ushort>(), () => new AkRtpc(Ar));
}

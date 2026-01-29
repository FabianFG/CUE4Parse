using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkRtpcGraphPoint
{
    public readonly float From;
    public readonly float To;
    public readonly ECurveInterpolation Interpolation;

    public AkRtpcGraphPoint(FArchive Ar)
    {
        From = Ar.Read<float>();
        To = Ar.Read<float>();
        Interpolation = (ECurveInterpolation) Ar.Read<uint>();
    }

    public static AkRtpcGraphPoint[] ReadArray(FArchive Ar) =>
        Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRtpcGraphPoint(Ar));
}

public readonly struct AkRtpc
{
    public readonly uint RtpcId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ERTPCType RtpcType;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ERTPCAccum RtpcAccum;
    public readonly int ParamId;
    public readonly uint RtpcCurveId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly ECurveScaling Scaling;
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkRtpc(FArchive Ar)
    {
        RtpcId = Ar.Read<uint>();

        if (WwiseVersions.Version > 89)
        {
            RtpcType = Ar.Read<ERTPCType>();
            RtpcAccum = Ar.Read<ERTPCAccum>();
        }

        if (WwiseVersions.Version <= 89)
        {
            ParamId = Ar.Read<int>();
        }
        else if (WwiseVersions.Version <= 113)
        {
            ParamId = Ar.Read<byte>();
        }
        else
        {
            ParamId = Ar.Read7BitEncodedInt();
        }

        RtpcCurveId = Ar.Read<uint>();

        if (WwiseVersions.Version <= 36)
        {
            Scaling = Ar.Read<ECurveScaling>();
        }
        else
        {
            Scaling = Ar.Read<ECurveScaling>();
        }

        GraphPoints = Ar.ReadArray(Ar.Read<ushort>(), () => new AkRtpcGraphPoint(Ar));
    }

    public static AkRtpc[] ReadArray(FArchive Ar) =>
        Ar.ReadArray(Ar.Read<ushort>(), () => new AkRtpc(Ar));
}

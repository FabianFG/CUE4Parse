using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkRtpcGraphPoint
{
    public readonly float From;
    public readonly float To;
    public readonly uint Interpolation;

    public AkRtpcGraphPoint(FArchive Ar)
    {
        From = Ar.Read<float>();
        To = Ar.Read<float>();
        Interpolation = Ar.Read<uint>();
    }

    public static List<AkRtpcGraphPoint> ReadMultiple(FArchive Ar)
    {
        uint pointsCount = Ar.Read<uint>();
        var graphPoints = new List<AkRtpcGraphPoint>((int)pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            graphPoints.Add(new AkRtpcGraphPoint(Ar));
        }

        return graphPoints;
    }
}

public class AkRtpc
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
    public readonly List<AkRtpcGraphPoint> GraphPoints;

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

        ushort pointsCount = Ar.Read<ushort>();
        GraphPoints = new List<AkRtpcGraphPoint>(pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            GraphPoints.Add(new AkRtpcGraphPoint(Ar));
        }
    }

    public static List<AkRtpc> ReadMultiple(FArchive Ar)
    {
        ushort numCurves = Ar.Read<ushort>();
        var rtpcEntries = new List<AkRtpc>(numCurves);
        for (int j = 0; j < numCurves; j++)
        {
            rtpcEntries.Add(new AkRtpc(Ar));
        }

        return rtpcEntries;
    }
}

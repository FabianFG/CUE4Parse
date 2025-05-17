using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkRTPCGraphPoint
{
    public float From { get; }
    public float To { get; }
    public uint Interpolation { get; }

    public AkRTPCGraphPoint(FArchive Ar)
    {
        From = Ar.Read<float>();
        To = Ar.Read<float>();
        Interpolation = Ar.Read<uint>();
    }

    public static List<AkRTPCGraphPoint> ReadMultiple(FArchive Ar)
    {
        uint pointsCount = Ar.Read<uint>();
        var graphPoints = new List<AkRTPCGraphPoint>((int)pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            graphPoints.Add(new AkRTPCGraphPoint(Ar));
        }

        return graphPoints;
    }
}

public class AkRTPC
{
    public uint RTPCId { get; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ERTPCType RTPCType { get; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ERTPCAccum RTPCAccum { get; }
    public int ParamId { get; }
    public uint RTPCCurveId { get; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ECurveScaling Scaling { get; }
    public List<AkRTPCGraphPoint> GraphPoints { get; }

    public AkRTPC(FArchive Ar, bool modulator = false)
    {
        RTPCId = Ar.Read<uint>();

        if (WwiseVersions.Version > 89)
        {
            RTPCType = Ar.Read<ERTPCType>();
            RTPCAccum = Ar.Read<ERTPCAccum>();
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

        RTPCCurveId = Ar.Read<uint>();

        if (WwiseVersions.Version <= 36)
        {
            Scaling = Ar.Read<ECurveScaling>();
        }
        else
        {
            Scaling = Ar.Read<ECurveScaling>();
        }

        ushort pointsCount = Ar.Read<ushort>();
        GraphPoints = new List<AkRTPCGraphPoint>(pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            GraphPoints.Add(new AkRTPCGraphPoint(Ar));
        }
    }

    public static List<AkRTPC> ReadMultiple(FArchive Ar, bool modulator = false)
    {
        ushort numCurves = Ar.Read<ushort>();
        var rtpcEntries = new List<AkRTPC>(numCurves);
        for (int j = 0; j < numCurves; j++)
        {
            rtpcEntries.Add(new AkRTPC(Ar, modulator));
        }

        return rtpcEntries;
    }
}

using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

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
}

public class AkRTPC
{
    public uint RTPCId { get; }
    public byte RTPCType { get; }
    public byte RTPCAccum { get; }
    public int ParamId { get; }
    public uint RTPCCurveId { get; }
    public byte Scaling { get; }
    public List<AkRTPCGraphPoint> GraphPoints { get; }

    public AkRTPC(FArchive Ar, bool modulator = false)
    {
        RTPCId = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 89)
        {
            RTPCType = Ar.Read<byte>();
            RTPCAccum = Ar.Read<byte>();
        }

        if (WwiseVersions.WwiseVersion <= 89)
        {
            ParamId = Ar.Read<int>();
        }
        else if (WwiseVersions.WwiseVersion <= 113)
        {
            ParamId = Ar.Read<byte>();
        }
        else
        {
            ParamId = Ar.Read7BitEncodedInt();
        }

        RTPCCurveId = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion <= 36)
        {
            Scaling = Ar.Read<byte>();
        }
        else
        {
            Scaling = Ar.Read<byte>();
        }

        ushort pointsCount = Ar.Read<ushort>();
        GraphPoints = new List<AkRTPCGraphPoint>(pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            GraphPoints.Add(new AkRTPCGraphPoint(Ar));
        }
    }
}

public class AkRTPCList : List<AkRTPC>
{
    public AkRTPCList(FArchive Ar, bool modulator = false)
    {
        ushort numCurves = Ar.Read<ushort>();
        for (int i = 0; i < numCurves; i++)
        {
            Add(new AkRTPC(Ar, modulator));
        }
    }
}

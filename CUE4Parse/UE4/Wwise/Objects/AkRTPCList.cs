using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkRTPCGraphPoint
{
    public float From { get; }
    public float To { get; }
    public uint Interpolation { get; }

    public AkRTPCGraphPoint(FArchive ar)
    {
        From = ar.Read<float>();
        To = ar.Read<float>();
        Interpolation = ar.Read<uint>();
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

    public AkRTPC(FArchive ar, bool modulator = false)
    {
        RTPCId = ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 89)
        {
            RTPCType = ar.Read<byte>();
            RTPCAccum = ar.Read<byte>();
        }

        if (WwiseVersions.WwiseVersion <= 89)
        {
            ParamId = ar.Read<int>();
        }
        else if (WwiseVersions.WwiseVersion <= 113)
        {
            ParamId = ar.Read<byte>();
        }
        else
        {
            ParamId = ar.Read7BitEncodedInt();
        }

        RTPCCurveId = ar.Read<uint>();

        if (WwiseVersions.WwiseVersion <= 36)
        {
            Scaling = ar.Read<byte>();
        }
        else
        {
            Scaling = ar.Read<byte>();
        }

        ushort pointsCount = ar.Read<ushort>();
        GraphPoints = new List<AkRTPCGraphPoint>(pointsCount);
        for (int j = 0; j < pointsCount; j++)
        {
            GraphPoints.Add(new AkRTPCGraphPoint(ar));
        }
    }
}

public class AkRTPCList : List<AkRTPC>
{
    public AkRTPCList(FArchive ar, bool modulator = false)
    {
        ushort numCurves = ar.Read<ushort>();
        for (int i = 0; i < numCurves; i++)
        {
            Add(new AkRTPC(ar, modulator));
        }
    }
}

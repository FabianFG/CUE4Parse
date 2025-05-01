using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Readers
{
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

        public AkRTPC(FArchive ar)
        {
            RTPCId = ar.Read<uint>();
            RTPCType = ar.Read<byte>();
            RTPCAccum = ar.Read<byte>();
            ParamId = ar.Read7BitEncodedInt();
            RTPCCurveId = ar.Read<uint>();
            Scaling = ar.Read<byte>();

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
        public AkRTPCList(FArchive ar)
        {
            ushort numCurves = ar.Read<ushort>();
            for (int i = 0; i < numCurves; i++)
            {
                Add(new AkRTPC(ar));
            }
        }
    }
}

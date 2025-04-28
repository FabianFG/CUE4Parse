using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public struct AkRTPCGraphPoint
    {
        public float From;
        public float To;
        public uint Interpolation;
    }

    public struct AkRTPC
    {
        public uint RTPCId;
        public byte RTPCType;
        public byte RTPCAccum;
        public int ParamId;
        public uint RTPCCurveId;
        public byte Scaling;
        public List<AkRTPCGraphPoint> GraphPoints;
    }

    public static class AkRTPCList
    {
        public static List<AkRTPC> ReadRTPCList(this FArchive Ar)
        {
            ushort numCurves = Ar.Read<ushort>();
            var rtpcs = new List<AkRTPC>(numCurves);

            for (int i = 0; i < numCurves; i++)
            {
                uint rtpcId = Ar.Read<uint>();
                byte rtpcType = Ar.Read<byte>();
                byte rtpcAccum = Ar.Read<byte>();
                int paramId = Ar.Read7BitEncodedInt();
                uint rtpcCurveId = Ar.Read<uint>();
                byte scaling = Ar.Read<byte>();

                ushort pointsCount = Ar.Read<ushort>();
                var graphPoints = new List<AkRTPCGraphPoint>(pointsCount);

                for (int j = 0; j < pointsCount; j++)
                {
                    float x = Ar.Read<float>();
                    float y = Ar.Read<float>();
                    uint curveType = Ar.Read<uint>();

                    graphPoints.Add(new AkRTPCGraphPoint
                    {
                        From = x,
                        To = y,
                        Interpolation = curveType
                    });
                }

                rtpcs.Add(new AkRTPC
                {
                    RTPCId = rtpcId,
                    RTPCType = rtpcType,
                    RTPCAccum = rtpcAccum,
                    ParamId = paramId,
                    RTPCCurveId = rtpcCurveId,
                    Scaling = scaling,
                    GraphPoints = graphPoints
                });
            }

            return rtpcs;
        }
    }
}

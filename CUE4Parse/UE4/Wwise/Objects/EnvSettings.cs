using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class EnvSettings
{
    public List<ConversionTableEntry> ConversionTableEntries { get; private set; }

    public EnvSettings(FArchive ar)
    {
        ConversionTableEntries = [];
        int maxY;
        int maxX;
        if (WwiseVersions.WwiseVersion <= 89)
        {
            maxX = 2;
            maxY = 2;
        }
        else if (WwiseVersions.WwiseVersion <= 150)
        {
            maxX = 2;
            maxY = 3;
        }
        else
        {
            maxX = 4;
            maxY = 3;
        }

        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
            {
                var curveEnabled = ar.Read<byte>();
                int curveSize;
                if (WwiseVersions.WwiseVersion <= 36)
                {
                    var curveScaling = ar.Read<uint>();
                    curveSize = (int) ar.Read<uint>();
                }
                else
                {
                    var curveScaling = ar.Read<byte>();
                    curveSize = ar.Read<ushort>();
                }

                var graphPoints = new List<AkRTPCGraphPoint>(curveSize);
                for (int t = 0; t < curveSize; t++)
                {
                    graphPoints.Add(new AkRTPCGraphPoint(ar));
                }

                ConversionTableEntries.Add(new ConversionTableEntry(curveEnabled, graphPoints));
            }
        }
    }

    public class ConversionTableEntry
    {
        public byte CurveEnabled { get; }
        public List<AkRTPCGraphPoint> GraphPoints { get; }

        public ConversionTableEntry(byte curveEnabled, List<AkRTPCGraphPoint> graphPoints)
        {
            CurveEnabled = curveEnabled;
            GraphPoints = graphPoints;
        }
    }
}

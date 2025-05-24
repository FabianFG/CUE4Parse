using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class EnvSettings
{
    public List<ConversionTableEntry> ConversionTableEntries { get; private set; }

    public EnvSettings(FArchive Ar)
    {
        ConversionTableEntries = [];
        int maxY;
        int maxX;
        if (WwiseVersions.Version <= 89)
        {
            maxX = 2;
            maxY = 2;
        }
        else if (WwiseVersions.Version <= 150)
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
                var curveEnabled = Ar.Read<byte>();
                int curveSize;
                if (WwiseVersions.Version <= 36)
                {
                    var curveScaling = Ar.Read<uint>();
                    curveSize = (int) Ar.Read<uint>();
                }
                else
                {
                    var curveScaling = Ar.Read<byte>();
                    curveSize = Ar.Read<ushort>();
                }

                var graphPoints = new List<AkRtpcGraphPoint>(curveSize);
                for (int t = 0; t < curveSize; t++)
                {
                    graphPoints.Add(new AkRtpcGraphPoint(Ar));
                }

                ConversionTableEntries.Add(new ConversionTableEntry(curveEnabled, graphPoints));
            }
        }
    }

    public class ConversionTableEntry
    {
        public byte CurveEnabled { get; }
        public List<AkRtpcGraphPoint> GraphPoints { get; }

        public ConversionTableEntry(byte curveEnabled, List<AkRtpcGraphPoint> graphPoints)
        {
            CurveEnabled = curveEnabled;
            GraphPoints = graphPoints;
        }
    }
}

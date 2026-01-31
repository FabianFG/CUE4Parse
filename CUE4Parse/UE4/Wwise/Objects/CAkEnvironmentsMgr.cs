using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class CAkEnvironmentsMgr
{
    public readonly ConversionTableEntry[] ConversionTableEntries = [];

    // CAkBankMgr::ProcessEnvSettingsChunk
    public CAkEnvironmentsMgr(FArchive Ar)
    {
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

        ConversionTableEntries = new ConversionTableEntry[maxX * maxY];
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
            {
                // CAkEnvironmentsMgr -> CAkAttenuation -> CAkConversionTable
                var curveEnabled = Ar.Read<byte>();
                int curveSize;
                if (WwiseVersions.Version <= 36)
                {
                    Ar.Read<uint>(); // CurveScaling
                    curveSize = (int) Ar.Read<uint>();
                }
                else
                {
                    Ar.Read<byte>(); // CurveScaling
                    curveSize = Ar.Read<ushort>();
                }

                var graphPoints = Ar.ReadArray(curveSize, () => new AkRtpcGraphPoint(Ar));
                ConversionTableEntries[(i * maxY) + j] = new ConversionTableEntry(curveEnabled, graphPoints);
            }
        }
    }

    public readonly struct ConversionTableEntry
    {
        public readonly byte CurveEnabled;
        public readonly AkRtpcGraphPoint[] GraphPoints;

        public ConversionTableEntry(byte curveEnabled, AkRtpcGraphPoint[] graphPoints)
        {
            CurveEnabled = curveEnabled;
            GraphPoints = graphPoints;
        }
    }
}

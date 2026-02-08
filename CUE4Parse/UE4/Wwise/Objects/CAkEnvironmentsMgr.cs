using CUE4Parse.UE4.Readers;
namespace CUE4Parse.UE4.Wwise.Objects;

public class CAkEnvironmentsMgr
{
    public readonly uint AttenuationId;
    public readonly CAkConversionTable[,]? ConversionTableEntries;

    // CAkBankMgr::ProcessEnvSettingsChunk
    public CAkEnvironmentsMgr(FArchive Ar)
    {
        if (WwiseVersions.Version > 154)
        {
            AttenuationId = Ar.Read<uint>();
            return; // Yes, that's it
        }

        (int maxX, int maxY) = WwiseVersions.Version switch
        {
            <= 89 => (2, 2),
            <= 150 => (2, 3),
            _ => (4, 3),
        };

        var ConversionTable = new CAkConversionTable[maxX, maxY];
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
            {
                var CurveEnabled = Ar.Read<byte>();
                ConversionTable[i, j] = new CAkConversionTable(Ar);
            }
        }
    }
}

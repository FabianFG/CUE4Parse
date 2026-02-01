using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Atmosphere;

public class UAtmosphericFogComponent : USkyAtmosphereComponent
{
    public FByteBulkData? TempTransmittanceData;
    public FByteBulkData? TempIrradianceData;
    public FByteBulkData? TempInscatterData;
    public int CounterVal;

    override public void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RemovedAtmosphericFog)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.ATMOSPHERIC_FOG_CACHE_DATA)
            {
                TempTransmittanceData = new FByteBulkData(Ar);
                if (TempTransmittanceData.Header.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
                    Ar.Position += TempTransmittanceData.Header.SizeOnDisk;
                TempIrradianceData = new FByteBulkData(Ar);
                if (TempIrradianceData.Header.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
                    Ar.Position += TempIrradianceData.Header.SizeOnDisk;
            }

            TempInscatterData = new FByteBulkData(Ar);
            if (TempInscatterData.Header.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
                Ar.Position += TempInscatterData.Header.SizeOnDisk;
            CounterVal = Ar.Read<int>();

            //	TransformMode = ESkyAtmosphereTransformMode::PlanetTopAtComponentTransform;
            //  SetWorldLocation(FVector(0.0f, 0.0f, -100000.0f));

            bStaticLightingBuiltGUID = new(1, 0, 0, 0);
        }
    }
}

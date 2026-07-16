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
                TempIrradianceData = new FByteBulkData(Ar);
            }

            TempInscatterData = new FByteBulkData(Ar);
            CounterVal = Ar.Read<int>();

            //	TransformMode = ESkyAtmosphereTransformMode::PlanetTopAtComponentTransform;
            //  SetWorldLocation(FVector(0.0f, 0.0f, -100000.0f));

            bStaticLightingBuiltGUID = new(1, 0, 0, 0);
        }
    }
}

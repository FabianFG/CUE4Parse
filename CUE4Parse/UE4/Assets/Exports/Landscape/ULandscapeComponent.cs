using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Landscape;

public class ULandscapeComponent : UPrimitiveComponent
{
    public FMeshMapBuildData LegacyMapBuildData;
    public FLandscapeComponentGrassData GrassData;
    public bool bCooked;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
        {
            LegacyMapBuildData.LightMap = new FLightMap(Ar);
            LegacyMapBuildData.ShadowMap = new FShadowMap(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_LANDSCAPE_GRASS_DATA)
        {
            GrassData = new FLandscapeComponentGrassData(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.LANDSCAPE_PLATFORMDATA_COOKING)
        {
            bCooked = Ar.ReadBoolean();
        }
    }
}

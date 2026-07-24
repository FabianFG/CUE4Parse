using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoPrimitiveComponent : FFastGeoComponent
{
    public FTransform LocalTransform;
    public FTransform WorldTransform;
    public FBoxSphereBounds LocalBounds;
    public FBoxSphereBounds WorldBounds;
    public bool bVisible;
    public bool bStaticWhenNotMoveable;
    public bool bFillCollisionUnderneathForNavmesh;
    public bool bRasterizeAsFilledConvexVolume;
    public bool bCanEverAffectNavigation;
    public bool bMultiBodyOverlap;
    public int SurrogateComponentDescriptorIndex;
    public float[] CustomPrimitiveData;
    public EDetailMode DetailMode;
    public EHasCustomNavigableGeometry bHasCustomNavigableGeometry;
    public FPackageIndex[]? RuntimeVirtualTextures;
    public FStructFallback? BodyInstance;
    public FSceneProxyDesc SceneProxyDesc;
    public object? CustomGameData;

    public FFastGeoPrimitiveComponent(FFastGeoArchive Ar) : base(Ar)
    {
        LocalTransform = new FTransform(Ar);
        WorldTransform = new FTransform(Ar);
        LocalBounds = new FBoxSphereBounds(Ar);
        WorldBounds = new FBoxSphereBounds(Ar);
        bVisible = Ar.ReadBoolean();
        bStaticWhenNotMoveable = Ar.ReadBoolean();
        if (Ar.Game is GAME_SilverPalace)
        {
            Ar.SkipArray<float>();
            Ar.Position += 4;
            BodyInstance = new FStructFallback();
            UObject.DeserializePropertiesTagged(BodyInstance.Properties, Ar, false);
            SceneProxyDesc = new FSceneProxyDesc();
            Ar.Position += 266;
            CustomGameData = (Ar.ReadFName(), Ar.ReadFString(), Ar.ReadFString());
            return;
        }
        bFillCollisionUnderneathForNavmesh = Ar.ReadBoolean();
        bRasterizeAsFilledConvexVolume = Ar.ReadBoolean();
        bCanEverAffectNavigation = Ar.ReadBoolean();
        bMultiBodyOverlap = Ar.Game >= GAME_UE5_8 && Ar.ReadBoolean();
        if (Ar.Game is GAME_LEGOBatmanLegacyoftheDarkKnight) Ar.Position += 4;
        if (Ar.Game is GAME_WutheringWaves)
        {
            bMultiBodyOverlap = Ar.ReadBoolean();
            SurrogateComponentDescriptorIndex = Ar.Read<int>();
            CustomPrimitiveData = Ar.ReadArray<float>();
            DetailMode = Ar.Read<EDetailMode>();
            bHasCustomNavigableGeometry = Ar.Read<EHasCustomNavigableGeometry>();
            RuntimeVirtualTextures = Ar.ReadArray(Ar.ReadFPackageIndex);
            SceneProxyDesc = new FSceneProxyDesc();
            Ar.Position += 377;
            return;
        }
        SurrogateComponentDescriptorIndex = Ar.Game >= GAME_UE5_8 ? Ar.Read<int>() : 0;
        CustomPrimitiveData = Ar.ReadArray<float>();
        DetailMode = Ar.Game is < GAME_UE5_8 ? Ar.Read<EDetailMode>() : EDetailMode.Low;
        bHasCustomNavigableGeometry = Ar.Read<EHasCustomNavigableGeometry>();
        RuntimeVirtualTextures = Ar.ReadArray(Ar.ReadFPackageIndex);
        BodyInstance = Ar.Game < GAME_UE5_8 ? new FStructFallback(Ar, "BodyInstance") : null;
        SceneProxyDesc = new FSceneProxyDesc(Ar);
    }
}

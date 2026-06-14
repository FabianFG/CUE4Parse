using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

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
    public FPackageIndex[] RuntimeVirtualTextures;
    public FStructFallback? BodyInstance;
    public FSceneProxyDesc SceneProxyDesc;

    public FFastGeoPrimitiveComponent(FFastGeoArchive Ar) : base(Ar)
    {
        LocalTransform = new FTransform(Ar);
        WorldTransform = new FTransform(Ar);
        LocalBounds = new FBoxSphereBounds(Ar);
        WorldBounds = new FBoxSphereBounds(Ar);
        bVisible = Ar.ReadBoolean();
        bStaticWhenNotMoveable = Ar.ReadBoolean();
        bFillCollisionUnderneathForNavmesh = Ar.ReadBoolean();
        bRasterizeAsFilledConvexVolume = Ar.ReadBoolean();
        bCanEverAffectNavigation = Ar.ReadBoolean();
        bMultiBodyOverlap = Ar.Game >= EGame.GAME_UE5_8 && Ar.ReadBoolean();
        if (Ar.Game is EGame.GAME_LEGOBatmanLegacyoftheDarkKnight) Ar.Position += 4;
        SurrogateComponentDescriptorIndex = Ar.Game >= EGame.GAME_UE5_8 ? Ar.Read<int>() : 0;
        CustomPrimitiveData = Ar.ReadArray<float>();
        DetailMode = Ar.Game is < EGame.GAME_UE5_8 or EGame.GAME_WutheringWavesFastGeo ? Ar.Read<EDetailMode>() : EDetailMode.Low;
        bHasCustomNavigableGeometry = Ar.Read<EHasCustomNavigableGeometry>();
        RuntimeVirtualTextures = Ar.ReadArray(Ar.ReadFPackageIndex);
        BodyInstance = Ar.Game < EGame.GAME_UE5_8 ? new FStructFallback(Ar, "BodyInstance") : null;
        if (Ar.Game != EGame.GAME_WutheringWavesFastGeo)
            SceneProxyDesc = new FSceneProxyDesc(Ar);
        else
        {
            SceneProxyDesc = new FSceneProxyDesc();
            Ar.Position += 365;
        }
    }
}

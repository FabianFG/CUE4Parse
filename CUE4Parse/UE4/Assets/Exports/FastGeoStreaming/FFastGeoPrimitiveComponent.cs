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
    public float[] CustomPrimitiveData;
    public EDetailMode DetailMode;
    public EHasCustomNavigableGeometry bHasCustomNavigableGeometry;
    public FPackageIndex[] RuntimeVirtualTextures;
    public FStructFallback BodyInstance;
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
        CustomPrimitiveData = Ar.ReadArray<float>();
        DetailMode = Ar.Read<EDetailMode>();
        bHasCustomNavigableGeometry = Ar.Read<EHasCustomNavigableGeometry>();
        RuntimeVirtualTextures = Ar.ReadArray(Ar.ReadFPackageIndex);
        BodyInstance = new FStructFallback(Ar, "BodyInstance");
        SceneProxyDesc = new FSceneProxyDesc(Ar);
    }
}

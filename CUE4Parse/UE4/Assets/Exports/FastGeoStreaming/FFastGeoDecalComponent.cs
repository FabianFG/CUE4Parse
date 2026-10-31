using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoDecalComponent : FFastGeoComponent
{
    public FVector DecalSize;
    public bool bIsVisible;
    public bool bDestroyOwnerAfterFade;
    public FDeferredDecalSceneProxyDesc SceneProxyDesc;

    public FFastGeoDecalComponent(FFastGeoArchive Ar) : base(Ar)
    {
        DecalSize = new FVector(Ar);
        var flags = Ar.Read<int>();
        bIsVisible = (flags & 1) != 0;
        bDestroyOwnerAfterFade = Ar.Game >= GAME_UE5_8 ? Ar.ReadBoolean() : (flags & 2) != 0; // some 5.7 games backported this component
        SceneProxyDesc = new FDeferredDecalSceneProxyDesc(Ar);
    }
}

public class FDeferredDecalSceneProxyDesc
{
    public FPackageIndex DecalMaterial;
    public FTransform TransformWithDecalScale = FTransform.Identity;
    public FBoxSphereBounds Bounds;

    public FLinearColor DecalColor;

    public float InitializationWorldTimeSeconds;
    public float FadeScreenSize;
    public float FadeDuration;
    public float FadeStartDelay;
    public float FadeInDuration;
    public float FadeInStartDelay;
    public int SortOrder = -1;

    public bool bDrawInGame;
    public bool bDrawInEditor;
    public bool bShouldFade  = true;

    public FDeferredDecalSceneProxyDesc(FFastGeoArchive Ar)
    {
        DecalMaterial = Ar.ReadFPackageIndex();
        TransformWithDecalScale = new FTransform(Ar);
        Bounds = new FBoxSphereBounds(Ar);
        DecalColor = Ar.Read<FLinearColor>();
        InitializationWorldTimeSeconds = Ar.Read<float>();
        FadeScreenSize = Ar.Read<float>();
        FadeDuration = Ar.Read<float>();
        FadeStartDelay = Ar.Read<float>();
        FadeInDuration = Ar.Read<float>();
        FadeInStartDelay = Ar.Read<float>();
        SortOrder = Ar.Read<int>();
        bDrawInGame = Ar.ReadBoolean();
        bDrawInEditor = Ar.ReadBoolean();
        bShouldFade = Ar.ReadBoolean();
    }
}

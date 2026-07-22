using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Animation;

public class UFortAnimNotifyState_SpawnProp : UAnimNotifyState
{
    // public EFortCustomPartType[] AttachPartOverrides;
    public FName? SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale = FVector.OneVector;
    public FPackageIndex? ActorProp;
    public bool bCanEditSkeletalMeshProp;
    public FPackageIndex? SkeletalMeshProp;
    public FPackageIndex? SkeletalMeshPropAnimation;
    public FPackageIndex? SkeletalMeshPropAnimClass;
    public bool bInheritScale;
    public bool bAbsoluteScale;
    public bool bUseAttachParentBound;
    public bool bPropAnimLooping;
    public bool bSyncMontage;
    public bool bPrestreamTextures;
    public float PrestreamTextureDuration;
    public FPackageIndex? StaticMeshProp;
    // public FEmotePropMaterialScalarParam?[]? PropMaterialScalarParams;
    // public int PropId;
    // public FGameplayTag? PropExclusionTag;
    // public FFortCosmeticOverlayMaterialData? OverlayMaterials;
    // public bool bIncludeInPawnHighlightSet;
    // public bool bRenderCustomDepth;
    // public int CustomDepthStencilValue;
    // public ERendererStencilMask CustomDepthStencilWriteMask;
    // public bool bTickAnimEvenWhenNotRendered;
    // public FSoftObjectPath? VariantsCosmeticItemDef;
    // public bool bApplyVariantsToSpawnedItems;
    // public bool bApplyMeshSwappingVariants;
    // public bool bUseVariantsFromEmoteLeader;
    // public bool bTrackComponentPropInGC;
    // public Dictionary<FPackageIndex, FPackageIndex?>? PersistComponents;
    // public FPackageIndex? LoadedVariantsCosmeticItemDef;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SocketName = GetOrDefault<FName?>(nameof(SocketName));
        LocationOffset = GetOrDefault<FVector>(nameof(LocationOffset));
        RotationOffset = GetOrDefault<FRotator>(nameof(RotationOffset));
        Scale = GetOrDefault(nameof(Scale), Scale);
        ActorProp = GetOrDefault<FPackageIndex>(nameof(ActorProp));
        bCanEditSkeletalMeshProp = GetOrDefault<bool>(nameof(bCanEditSkeletalMeshProp));
        SkeletalMeshProp = GetOrDefault<FPackageIndex>(nameof(SkeletalMeshProp));
        SkeletalMeshPropAnimation = GetOrDefault<FPackageIndex>(nameof(SkeletalMeshPropAnimation));
        SkeletalMeshPropAnimClass = GetOrDefault<FPackageIndex>(nameof(SkeletalMeshPropAnimClass));
        bInheritScale = GetOrDefault<bool>(nameof(bInheritScale));
        bAbsoluteScale = GetOrDefault<bool>(nameof(bAbsoluteScale));
        bUseAttachParentBound = GetOrDefault<bool>(nameof(bUseAttachParentBound));
        bPropAnimLooping = GetOrDefault<bool>(nameof(bPropAnimLooping));
        bSyncMontage = GetOrDefault<bool>(nameof(bSyncMontage));
        bPrestreamTextures = GetOrDefault<bool>(nameof(bPrestreamTextures));
        PrestreamTextureDuration = GetOrDefault<float>(nameof(PrestreamTextureDuration));
        StaticMeshProp = GetOrDefault<FPackageIndex>(nameof(StaticMeshProp));
    }
}

public enum EFortCustomPartType : byte
{
    Head,
    Body,
    Hat,
    Backpack,
    MiscOrTail,
    Face,
    Gameplay,
    ExtraPart,
    Gameplay2,
    NumTypes,
    EFortCustomPartType_MAX,
}

public enum ERendererStencilMask : byte
{
    ERSM_Default,
    ERSM_255,
    ERSM_1,
    ERSM_2,
    ERSM_4,
    ERSM_8,
    ERSM_16,
    ERSM_32,
    ERSM_64,
    ERSM_128,
    ERSM_MAX,
}

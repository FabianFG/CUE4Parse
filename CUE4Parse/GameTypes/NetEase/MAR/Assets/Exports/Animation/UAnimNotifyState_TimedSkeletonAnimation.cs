using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Assets.Exports.Animation;

public class UAnimNotifyState_TimedSkeletonAnimation : UAnimNotifyState
{
    public FSoftObjectPath? SkeletalMeshTemplate;
    public FSoftObjectPath?[]? OverrideMaterials;
    public FName? TargetComponentTag;
    public FName? SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public bool bDoNotAttach;
    public bool bDoNotUpdateLocation;
    public bool bDoNotUpdateRotation;
    public bool bDoNotUpdateScale;
    public bool bVisibleDuringAnimNotifyState;
    public bool bDelayHiddenOnPaused;
    public bool bIsRenderCustomDepth;
    public bool bReceiveDecal;
    public EVisibilityBasedAnimTickOption VisibilityBasedAnimTickOption;
    public bool bDoNotTickWhenInvisibleOrHiddenInGame;
    public FSoftObjectPath? AnimToPlay;
    public bool bIsLoop;
    public bool bSyncAnimPosFromNotify;
    public bool bSyncMontageSection;
    public float AnimStartPos;
    public bool bSkeletalUseAttachParentBound;
    public bool bCustomLightingChannels;
    public FLightingChannels? LightingChannels;
    public FPackageIndex? DelayHandleSkeletaMesh;
    public FPackageIndex? OwnerMeshActor;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SkeletalMeshTemplate = GetOrDefault<FSoftObjectPath>(nameof(SkeletalMeshTemplate));
        OverrideMaterials = GetOrDefault<FSoftObjectPath?[]>(nameof(OverrideMaterials));
        TargetComponentTag = GetOrDefault<FName>(nameof(TargetComponentTag));
        SocketName = GetOrDefault<FName>(nameof(SocketName));
        LocationOffset = GetOrDefault<FVector>(nameof(LocationOffset));
        RotationOffset = GetOrDefault<FRotator>(nameof(RotationOffset));
        bDoNotAttach = GetOrDefault<bool>(nameof(bDoNotAttach));
        bDoNotUpdateLocation = GetOrDefault<bool>(nameof(bDoNotUpdateLocation));
        bDoNotUpdateRotation = GetOrDefault<bool>(nameof(bDoNotUpdateRotation));
        bDoNotUpdateScale = GetOrDefault<bool>(nameof(bDoNotUpdateScale));
        bVisibleDuringAnimNotifyState = GetOrDefault<bool>(nameof(bVisibleDuringAnimNotifyState));
        bDelayHiddenOnPaused = GetOrDefault<bool>(nameof(bDelayHiddenOnPaused));
        bIsRenderCustomDepth = GetOrDefault<bool>(nameof(bIsRenderCustomDepth));
        bReceiveDecal = GetOrDefault<bool>(nameof(bReceiveDecal));
        VisibilityBasedAnimTickOption = GetOrDefault<EVisibilityBasedAnimTickOption>(nameof(VisibilityBasedAnimTickOption));
        bDoNotTickWhenInvisibleOrHiddenInGame = GetOrDefault<bool>(nameof(bDoNotTickWhenInvisibleOrHiddenInGame));
        AnimToPlay = GetOrDefault<FSoftObjectPath>(nameof(AnimToPlay));
        bIsLoop = GetOrDefault<bool>(nameof(bIsLoop));
        bSyncAnimPosFromNotify = GetOrDefault<bool>(nameof(bSyncAnimPosFromNotify));
        bSyncMontageSection = GetOrDefault<bool>(nameof(bSyncMontageSection));
        AnimStartPos = GetOrDefault<float>(nameof(AnimStartPos));
        bSkeletalUseAttachParentBound = GetOrDefault<bool>(nameof(bSkeletalUseAttachParentBound));
        bCustomLightingChannels = GetOrDefault<bool>(nameof(bCustomLightingChannels));
        LightingChannels = GetOrDefault<FLightingChannels>(nameof(LightingChannels));
        DelayHandleSkeletaMesh = GetOrDefault<FPackageIndex>(nameof(DelayHandleSkeletaMesh));
        OwnerMeshActor = GetOrDefault<FPackageIndex>(nameof(OwnerMeshActor));
    }
}

public enum EVisibilityBasedAnimTickOption : byte
{
    AlwaysTickPoseAndRefreshBones,
    AlwaysTickPose,
    OnlyTickMontagesWhenNotRendered,
    OnlyTickPoseWhenRendered,
    EVisibilityBasedAnimTickOption_MAX,
}

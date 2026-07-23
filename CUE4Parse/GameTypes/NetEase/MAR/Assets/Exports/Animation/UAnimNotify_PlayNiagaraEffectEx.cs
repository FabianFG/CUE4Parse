using CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Assets.Exports.Animation;

public class UAnimNotify_PlayNiagaraEffectEx : UAnimNotify_PlayNiagaraEffect
{
    public bool bCastShadow;
    public bool bRenderCustomDepthPass;
    public bool bDoNotUpdateRotation;
    public bool bAutoPossessAttachment;
    public FName?[]? NiagaraTags;
    public bool bBlockOnTagsOrActiveTags;
    public FGameplayTagContainer? BlockTags;
    public FGameplayTagContainer? ActiveTags;
    public bool bEnableFXSlomo;
    public float SlomoTimeScale;
    public FLightingChannels? LightingChannels;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bCastShadow = GetOrDefault<bool>(nameof(bCastShadow));
        bRenderCustomDepthPass = GetOrDefault<bool>(nameof(bRenderCustomDepthPass));
        bDoNotUpdateRotation = GetOrDefault<bool>(nameof(bDoNotUpdateRotation));
        bAutoPossessAttachment = GetOrDefault<bool>(nameof(bAutoPossessAttachment));
        NiagaraTags = GetOrDefault<FName?[]?>(nameof(NiagaraTags));
        bBlockOnTagsOrActiveTags = GetOrDefault<bool>(nameof(bBlockOnTagsOrActiveTags));
        BlockTags = GetOrDefault<FGameplayTagContainer?>(nameof(BlockTags));
        ActiveTags = GetOrDefault<FGameplayTagContainer?>(nameof(ActiveTags));
        bEnableFXSlomo = GetOrDefault<bool>(nameof(bEnableFXSlomo));
        SlomoTimeScale = GetOrDefault<float>(nameof(SlomoTimeScale));
        LightingChannels = GetOrDefault<FLightingChannels?>(nameof(LightingChannels));
    }
}

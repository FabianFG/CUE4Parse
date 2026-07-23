using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Assets.Exports.Animation;

public class UAnimNotify_PlayNiagaraEffect : UAnimNotify
{
    public FSoftObjectPath? Template;
    public bool bUseCombineEffect;
    public FSoftObjectPath? TemplateCombineEffect;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale = FVector.OneVector;
    public bool bAbsoluteScale;
    public bool bDeactivateOnMontageEnded;
    public bool Attached;
    public FName? SocketName;
    public float EffectLastTime;
    public float ComponentTimeScale;
    // public Dictionary<FName, float>? FloatUserParameterValues;
    // public Dictionary<FName, FVector>? VectorUserParameterValues;
    // public Dictionary<FName, FLinearColor>? ColorUserParameterValues;
    public bool bUsedCustomStencil;
    public int CustomStencilValue;
    public bool bUsedTranslucencySortPriority;
    public int TranslucencySortPriority;
    public bool bUsedTranslucencySortDistanceOffset;
    public float TranslucencySortDistanceOffset;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Template = GetOrDefault<FSoftObjectPath?>(nameof(Template));
        bUseCombineEffect = GetOrDefault<bool>(nameof(bUseCombineEffect));
        TemplateCombineEffect = GetOrDefault<FSoftObjectPath?>(nameof(TemplateCombineEffect));
        LocationOffset = GetOrDefault<FVector>(nameof(LocationOffset));
        RotationOffset = GetOrDefault<FRotator>(nameof(RotationOffset));
        Scale = GetOrDefault(nameof(Scale), Scale);
        bAbsoluteScale = GetOrDefault<bool>(nameof(bAbsoluteScale));
        bDeactivateOnMontageEnded = GetOrDefault<bool>(nameof(bDeactivateOnMontageEnded));
        Attached = GetOrDefault<bool>(nameof(Attached));
        SocketName = GetOrDefault<FName?>(nameof(SocketName));
        EffectLastTime = GetOrDefault<float>(nameof(EffectLastTime));
        ComponentTimeScale = GetOrDefault<float>(nameof(ComponentTimeScale));
        //
        bUsedCustomStencil = GetOrDefault<bool>(nameof(bUsedCustomStencil));
        CustomStencilValue = GetOrDefault<int>(nameof(CustomStencilValue));
        bUsedTranslucencySortPriority = GetOrDefault<bool>(nameof(bUsedTranslucencySortPriority));
        TranslucencySortPriority = GetOrDefault<int>(nameof(TranslucencySortPriority));
        bUsedTranslucencySortDistanceOffset = GetOrDefault<bool>(nameof(bUsedTranslucencySortDistanceOffset));
        TranslucencySortDistanceOffset = GetOrDefault<float>(nameof(TranslucencySortDistanceOffset));
    }
}

using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Animation;

public class UAnimNotifyState_TimedNiagaraEffect : UAnimNotifyState
{
    public FPackageIndex? Template;
    public FName? SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale = FVector.OneVector;
    public bool bApplyRateScaleAsTimeDilation;
    public bool bDestroyAtEnd;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Template = GetOrDefault<FPackageIndex?>(nameof(Template));
        SocketName = GetOrDefault<FName?>(nameof(SocketName));
        LocationOffset = GetOrDefault<FVector>(nameof(LocationOffset));
        RotationOffset = GetOrDefault<FRotator>(nameof(RotationOffset));
        Scale = GetOrDefault(nameof(Scale), Scale);
        bApplyRateScaleAsTimeDilation = GetOrDefault<bool>(nameof(bApplyRateScaleAsTimeDilation));
        bDestroyAtEnd = GetOrDefault<bool>(nameof(bDestroyAtEnd));
    }
}

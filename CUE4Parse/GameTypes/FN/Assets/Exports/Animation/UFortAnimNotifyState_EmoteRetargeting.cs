using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Animation;

public class UFortAnimNotifyState_EmoteRetargeting : UAnimNotifyState
{
    public FEmoteRetargetingNotifyParameters[]? EmoteParameters;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EmoteParameters = GetOrDefault<FEmoteRetargetingNotifyParameters[]?>(nameof(EmoteParameters));
    }
}

[StructFallback]
public readonly struct FEmoteRetargetingNotifyParameters : IUStruct
{
    public readonly EFortPlayerAnimBodyType BodyTypeToAffect;
    public readonly EFortHandIKOverrideType LeftHandIK;
    public readonly EFortHandIKOverrideType RightHandIK;

    public FEmoteRetargetingNotifyParameters(FStructFallback fallback)
    {
        BodyTypeToAffect = fallback.GetOrDefault<EFortPlayerAnimBodyType>(nameof(BodyTypeToAffect));
        LeftHandIK = fallback.GetOrDefault<EFortHandIKOverrideType>(nameof(LeftHandIK));
        RightHandIK = fallback.GetOrDefault<EFortHandIKOverrideType>(nameof(RightHandIK));
    }
}

public enum EFortPlayerAnimBodyType : byte
{
    Small,
    Medium,
    Large,
    All,
    EFortPlayerAnimBodyType_MAX
}

public enum EFortHandIKOverrideType : byte
{
    UseDefault,
    ForceFK,
    ForceIK,
    ForceFKSnap,
    EFortHandIKOverrideType_MAX
}

using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Assets.Exports.Animation;

public class UAN_AkEvent : UMarvelAnimNotify
{
    public FName? AttachName;
    public FSoftObjectPath? Event;
    public bool bReplay;
    public bool bEndWithAbility;
    public bool bEndWithMontage;
    public EAkFadeInterpolation FadeoutType;
    public float FadeoutTime;
    public bool bTargetSelf;
    public bool bTargetTeammate;
    public bool bTargetEnemy;
    public bool bSetSwitch;
    public FName? SwitchGroup;
    public FName? SwitchState;
    public EMarvelAudioType AudioType;
    public bool bCustomSocket;
    public FPackageIndex? AudioEvent;
    public FPackageIndex? CachedMeshComp;
    public FPackageIndex? CachedAnimation;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AttachName = GetOrDefault<FName?>(nameof(AttachName));
        Event = GetOrDefault<FSoftObjectPath?>(nameof(Event));
        bReplay = GetOrDefault<bool>(nameof(bReplay));
        bEndWithAbility = GetOrDefault<bool>(nameof(bEndWithAbility));
        bEndWithMontage = GetOrDefault<bool>(nameof(bEndWithMontage));
        FadeoutType = GetOrDefault<EAkFadeInterpolation>(nameof(FadeoutType));
        FadeoutTime = GetOrDefault<float>(nameof(FadeoutTime));
        bTargetSelf = GetOrDefault<bool>(nameof(bTargetSelf));
        bTargetTeammate = GetOrDefault<bool>(nameof(bTargetTeammate));
        bTargetEnemy = GetOrDefault<bool>(nameof(bTargetEnemy));
        bSetSwitch = GetOrDefault<bool>(nameof(bSetSwitch));
        SwitchGroup = GetOrDefault<FName?>(nameof(SwitchGroup));
        SwitchState = GetOrDefault<FName?>(nameof(SwitchState));
        AudioType = GetOrDefault<EMarvelAudioType>(nameof(AudioType));
        bCustomSocket = GetOrDefault<bool>(nameof(bCustomSocket));
        AudioEvent = GetOrDefault<FPackageIndex?>(nameof(AudioEvent));
        CachedMeshComp = GetOrDefault<FPackageIndex?>(nameof(CachedMeshComp));
        CachedAnimation = GetOrDefault<FPackageIndex?>(nameof(CachedAnimation));
    }
}

public enum EAkFadeInterpolation : byte
{
    AKFI_Log3,
    AKFI_Sine,
    AKFI_Log1,
    AKFI_InvSCurve,
    AKFI_Linear,
    AKFI_SCurve,
    AKFI_Exp1,
    AKFI_SineRecip,
    AKFI_Exp3,
    AKFI_MAX
}

public enum EMarvelAudioType : byte
{
    MAT_None,
    MAT_Foley,
    MAT_Ability,
    MAT_Weapon,
    MAT_Impact,
    MAT_Breath,
    MAT_HeroVocal,
    MAT_SystemVocal,
    MAT_Music,
    MAT_UI,
    MAT_Ambience,
    MAT_Broken,
    MAT_Modes,
    MAT_Hit,
    MAT_Critical,
    MAT_Max,
    MAT_Max_0
}

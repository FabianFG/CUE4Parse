using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Animation;

public class UFortAnimNotifyState_EmoteSound : UAnimNotifyState
{
    public FPackageIndex? EmoteSound1P;
    public FPackageIndex? EmoteSound3P;
    public FGameplayTag? MusicEventTagOverride1P;
    public FGameplayTag? MusicEventTagOverride3P;
    public bool bPrimarySound;
    public FName? SoundName;
    public float FadeOutTime;
    public bool bEmoteLeaderOnly;
    public bool bStartSoundRelativeToNotifyBeginTime;
    public bool bStopAudioOnNotifyEnd;
    public FName? AttachName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EmoteSound1P = GetOrDefault<FPackageIndex?>(nameof(EmoteSound1P));
        EmoteSound3P = GetOrDefault<FPackageIndex?>(nameof(EmoteSound3P));
        MusicEventTagOverride1P = GetOrDefault<FGameplayTag?>(nameof(MusicEventTagOverride1P));
        MusicEventTagOverride3P = GetOrDefault<FGameplayTag?>(nameof(MusicEventTagOverride3P));
        bPrimarySound = GetOrDefault<bool>(nameof(bPrimarySound));
        SoundName = GetOrDefault<FName?>(nameof(SoundName));
        FadeOutTime = GetOrDefault<float>(nameof(FadeOutTime));
        bEmoteLeaderOnly = GetOrDefault<bool>(nameof(bEmoteLeaderOnly));
        bStartSoundRelativeToNotifyBeginTime = GetOrDefault<bool>(nameof(bStartSoundRelativeToNotifyBeginTime));
        bStopAudioOnNotifyEnd = GetOrDefault<bool>(nameof(bStopAudioOnNotifyEnd));
        AttachName = GetOrDefault<FName?>(nameof(AttachName));
    }
}

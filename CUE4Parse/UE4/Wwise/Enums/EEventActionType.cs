namespace CUE4Parse.UE4.Wwise.Enums
{
    public enum EEventActionType: byte
    {
        Stop = 0x01,
        Pause,
        Resume,
        Play,
        Trigger,
        Mute,
        UnMute,
        SetVoicePitch,
        ResetVoicePitch,
        SetVoiceVolume,
        ResetVoiceVolume,
        SetBusVolume,
        ResetBusVolume,
        SetVoiceLowPassFilter,
        ResetVoiceLowPassFilter,
        EnableState,
        DisableState,
        SetState,
        SetGameParameter,
        ResetGameParameter,
        SetSwitch,
        ToggleBypassEffect,
        ResetBypassEffect,
        Break,
        Seek = 0x1E,
    }
}

namespace CUE4Parse.UE4.Wwise.Enums;

public enum EEventActionType : byte
{
    Stop = 0x01, // AkActionStop
    Pause = 0x02, // AkActionPause
    Resume = 0x03, // AkActionResume
    Play = 0x04, // AkActionPlay
    PlayAndContinue = 0x05, // AkActionPlayAndContinue (early, removed in later versions)
    Mute = 0x06, // AkActionMute
    UnMute = 0x07, // AkActionMute
    SetVoicePitch = 0x08, // AkActionSetAkProp
    ResetVoicePitch = 0x09, // AkActionSetAkProp
    SetVoiceVolume = 0x0A, // AkActionSetAkProp
    ResetVoiceVolume = 0x0B, // AkActionSetAkProp
    SetBusVolume = 0x0C, // AkActionSetAkProp
    ResetBusVolume = 0x0D, // AkActionSetAkProp
    SetVoiceLowPassFilter = 0x0E, // AkActionSetAkProp
    ResetVoiceLowPassFilter = 0x0F, // AkActionSetAkProp
    EnableState = 0x10, // AkActionUseState
    DisableState = 0x11, // AkActionUseState
    SetState = 0x12, // AkActionSetState
    SetGameParameter = 0x13, // AkActionSetGameParameter
    ResetGameParameter = 0x14, // AkActionSetGameParameter
    Event = 0x15, // AkActionEvent
    SetSwitch = 0x19, //CAkActionSetSwitch
    ToggleBypassEffect = 0x1A, // AkActionBypassFX
    ResetBypassEffect = 0x1B, // AkActionBypassFX
    Break = 0x1C, // AkActionBreak
    Trigger = 0x1D, // AkActionTrigger
    Seek = 0x1E, // AkActionSeek
    Release = 0x1F, // AkActionRelease
    SetHighPassFilter = 0x20, // AkActionSetAkProp
    PlayEvent = 0x21, // AkActionPlayEvent
    ResetPlaylist = 0x22, // AkActionResetPlaylist
    PlayEventUnknown = 0x23, // AkActionPlayEventUnknow
    SetHighPassFilter2 = 0x30, // AkActionSetAkProp
    SetEffect = 0x31, // AkActionSetFX
    ResetEffect = 0x32, // AkActionSetFX
    BypassEffect = 0x33, // AkActionBypassFX
    ResetBypassEffect2 = 0x34, // AkActionBypassFX
    BypassEffect2 = 0x35, // AkActionBypassFX
    ResetBypassEffect3 = 0x36, // AkActionBypassFX
    ToggleBypassEffect2 = 0x37, // AkActionBypassFX
}

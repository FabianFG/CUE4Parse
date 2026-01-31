namespace CUE4Parse.UE4.Wwise.Enums;

// Split into two enums EAkActionType - EAkActionScope for convience
// High byte is EAkActionType, low byte is EAkActionScope
public enum EAkActionType : byte
{
    None = 0x0,
    Stop = 0x01, // AkActionStop
    // Stop_E_O = 0x0103,
    // Stop_ALL = 0x0104,
    // Stop_ALL_O = 0x0105,
    Pause = 0x02, // AkActionPause
    // Pause_E = 0x0202,
    // Pause_E_O = 0x0203,
    // Pause_ALL = 0x0204,
    // Pause_ALL_O = 0x0205,
    Resume = 0x03, // AkActionResume
    // Resume_E = 0x0302,
    // Resume_E_O = 0x0303,
    // Resume_ALL = 0x0304,
    // Resume_ALL_O = 0x0305,
    Play = 0x04, // AkActionPlay
    PlayAndContinue = 0x05, // AkActionPlayAndContinue (early, removed in later versions)
    Mute = 0x06, // AkActionMute
    // Mute_M = 0x0602,
    // Mute_O = 0x0603,
    UnMute = 0x07, // AkActionMute
    // Unmute_M = 0x0702,
    // Unmute_O = 0x0703,
    // Unmute_ALL = 0x0704,
    // Unmute_ALL_O = 0x0705,
    SetVoicePitch = 0x08, // AkActionSetAkProp
    // SetPitch_M = 0x0802,
    // SetPitch_O = 0x0803,
    ResetVoicePitch = 0x09, // AkActionSetAkProp
    // ResetPitch_M = 0x0902,
    // ResetPitch_O = 0x0903,
    // ResetPitch_ALL = 0x0904,
    // ResetPitch_ALL_O = 0x0905,
    SetVoiceVolume = 0x0A, // AkActionSetAkProp
    // SetVolume_M = 0x0A02,
    // SetVolume_O = 0x0A03,
    ResetVoiceVolume = 0x0B, // AkActionSetAkProp
    // ResetVolume_M = 0x0B02,
    // ResetVolume_O = 0x0B03,
    // ResetVolume_ALL = 0x0B04,
    // ResetVolume_ALL_O = 0x0B05,
    SetBusVolume = 0x0C, // AkActionSetAkProp
    // SetBusVolume_M = 0x0C02,
    // SetBusVolume_O = 0x0C03,
    ResetBusVolume = 0x0D, // AkActionSetAkProp
    // ResetBusVolume_M = 0x0D02,
    // ResetBusVolume_O = 0x0D03,
    // ResetBusVolume_ALL = 0x0D04,
    SetVoiceLowPassFilter = 0x0E, // AkActionSetAkProp
    // SetLPF_M = 0x0E02,
    // SetLPF_O = 0x0E03,
    ResetVoiceLowPassFilter = 0x0F, // AkActionSetAkProp
    // ResetLPF_M = 0x0F02,
    // ResetLPF_O = 0x0F03,
    // ResetLPF_ALL = 0x0F04,
    // ResetLPF_ALL_O = 0x0F05,
    EnableState = 0x10, // AkActionUseState
    DisableState = 0x11, // AkActionUseState
    SetState = 0x12, // AkActionSetState
    SetGameParameter = 0x13, // AkActionSetGameParameter
    // SetGameParameter = 0x1302,
    // SetGameParameter_O = 0x1303,
    ResetGameParameter = 0x14, // AkActionSetGameParameter
    // ResetGameParameter = 0x1402,
    // ResetGameParameter_O = 0x1403,
    Event = 0x15, // AkActionEvent
    Duck = 0x18, // AkActionDuck
    SetSwitch = 0x19, // AkActionSetSwitch
    Break = 0x1A, // AkActionBreak // <150 AkActionBypassFX
    // Break_E = 0x1A02,
    // Break_E_O = 0x1A03,
    Trigger = 0x1B, // AkActionTrigger // <150 AkActionBypassFX
    // Trigger = 0x1B00,
    // Trigger_O = 0x1B01,
    Break_v72_to_v150 = 0x1C, // AkActionBreak // >=150 removed
    Trigger_v72_to_v150 = 0x1D, // AkActionTrigger // >=150 removed
    Seek = 0x1E, // AkActionSeek
    // Seek_E = 0x1E02,
    // Seek_E_O = 0x1E03,
    // Seek_ALL = 0x1E04,
    // Seek_ALL_O = 0x1E05,
    Release = 0x1F, // AkActionRelease
    // Release = 0x1F02,
    // Release_O = 0x1F03,
    SetHighPassFilter = 0x20, // AkActionSetAkProp
    // SetHPF_M = 0x2002,
    // SetHPF_O = 0x2003,
    PlayEvent = 0x21, // AkActionPlayEvent
    ResetPlaylist = 0x22, // AkActionResetPlaylist
    // ResetPlaylist_E = 0x2202,
    // ResetPlaylist_E_O = 0x2203,
    PlayEventUnknown = 0x23, // AkActionPlayEventUnknow
    ResetHighPassFilter = 0x30, // AkActionSetAkProp
    // ResetHPF_M = 0x3002,
    // ResetHPF_O = 0x3003,
    // ResetHPF_ALL = 0x3004,
    // ResetHPF_ALL_O = 0x3005,
    SetEffect = 0x31, // AkActionSetFX
    ResetEffect = 0x32, // AkActionSetFX
    // ResetSetFX_M = 0x3202,
    // ResetSetFX_ALL = 0x3204,
    SetBypassEffectSlot = 0x33, // AkActionBypassFX
    // BypassFXSlot_M = 0x3302,
    // BypassFXSlot_O = 0x3303,
    ResetBypassEffectSlot = 0x34, // AkActionBypassFX
    // ResetBypassFXSlot_M = 0x3402,
    // ResetBypassFXSlot_O = 0x3403,
    // ResetBypassFXSlot_ALL = 0x3404,
    // ResetBypassFXSlot_ALL_O = 0x3405,
    SetBypassAllEffects = 0x35, // AkActionBypassFX
    // SetBypassAllFX_M = 0x3502,
    // SetBypassAllFX_O = 0x3503,
    ResetBypassEffects = 0x36, // AkActionBypassFX
    // ResetBypassAllFX_M = 0x3602,
    // ResetBypassAllFX_O = 0x3603,
    // ResetBypassAllFX_ALL = 0x3604,
    // ResetBypassAllFX_ALL_O = 0x3605,
    ResetAlllBypassEffects = 0x37, // AkActionBypassFX
    // ResetAllBypassFX_M = 0x3702,
    // ResetAllBypassFX_O = 0x3703,
    // ResetAllBypassFX_ALL = 0x3704,
    // ResetAllBypassFX_ALL_O = 0x3705,
    NoOp = 0x40
}

public enum EEventActionType_v72_to_v150 : byte
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
    SetSwitch = 0x19, // AkActionSetSwitch
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

public static class EventActionTypeExtensions
{
    public static string ToString(this EAkActionType actionType, uint version)
    {
        return version switch
        {
            < 150 => ((EEventActionType_v72_to_v150) (byte) actionType).ToString(),
            _ => actionType.ToString()
        };
    }
}

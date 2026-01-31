namespace CUE4Parse.UE4.Wwise.Enums;

// Versions > 125, default
public enum EAKBKHircType : byte
{
    State = 0x01, // Removed
    SoundSfxVoice,
    EventAction,
    Event,
    RandomSequenceContainer,
    SwitchContainer,
    ActorMixer,
    AudioBus,
    LayerContainer,
    MusicSegment,
    MusicTrack,
    MusicSwitchContainer,
    MusicRandomSequenceContainer,
    Attenuation,
    DialogueEvent,
    FxShareSet,
    FxCustom,
    AuxiliaryBus,
    LFO,
    Envelope,
    AudioDevice,
    TimeMod,
    SidechainMix // >= 168
}

// Versions <= 125
public enum EAKBKHircType_v125 : byte
{
    Settings = 0x01,
    SoundSfxVoice,
    EventAction,
    Event,
    RandomSequenceContainer,
    SwitchContainer,
    ActorMixer,
    AudioBus,
    LayerContainer,
    MusicSegment,
    MusicTrack,
    MusicSwitchContainer,
    MusicRandomSequenceContainer,
    Attenuation,
    DialogueEvent,
    FeedbackBus,
    FeedbackNode,
    FxShareSet,
    FxCustom,
    AuxiliaryBus,
    LFO,
    Envelope,
    AudioDevice
}

public static class EHierarchyObjectTypeExtensions
{
    public static string ToVersionString(this EAKBKHircType type)
    {
        return WwiseVersions.Version switch
        {
            <= 125 => ((EAKBKHircType_v125) type).ToString(),
            _ => type.ToString()
        };
    }

    public static EAKBKHircType MapToCurrent(this byte rawType)
    {
        if (WwiseVersions.Version > 125)
            return (EAKBKHircType) rawType;

        return (EAKBKHircType_v125) rawType switch
        {
            EAKBKHircType_v125.Settings => EAKBKHircType.State,
            EAKBKHircType_v125.SoundSfxVoice => EAKBKHircType.SoundSfxVoice,
            EAKBKHircType_v125.EventAction => EAKBKHircType.EventAction,
            EAKBKHircType_v125.Event => EAKBKHircType.Event,
            EAKBKHircType_v125.RandomSequenceContainer => EAKBKHircType.RandomSequenceContainer,
            EAKBKHircType_v125.SwitchContainer => EAKBKHircType.SwitchContainer,
            EAKBKHircType_v125.ActorMixer => EAKBKHircType.ActorMixer,
            EAKBKHircType_v125.AudioBus => EAKBKHircType.AudioBus,
            EAKBKHircType_v125.LayerContainer => EAKBKHircType.LayerContainer,
            EAKBKHircType_v125.MusicSegment => EAKBKHircType.MusicSegment,
            EAKBKHircType_v125.MusicTrack => EAKBKHircType.MusicTrack,
            EAKBKHircType_v125.MusicSwitchContainer => EAKBKHircType.MusicSwitchContainer,
            EAKBKHircType_v125.MusicRandomSequenceContainer => EAKBKHircType.MusicRandomSequenceContainer,
            EAKBKHircType_v125.Attenuation => EAKBKHircType.Attenuation,
            EAKBKHircType_v125.DialogueEvent => EAKBKHircType.DialogueEvent,
            EAKBKHircType_v125.FxShareSet => EAKBKHircType.FxShareSet,
            EAKBKHircType_v125.FxCustom => EAKBKHircType.FxCustom,
            EAKBKHircType_v125.AuxiliaryBus => EAKBKHircType.AuxiliaryBus,
            EAKBKHircType_v125.LFO => EAKBKHircType.LFO,
            EAKBKHircType_v125.Envelope => EAKBKHircType.Envelope,
            EAKBKHircType_v125.AudioDevice => EAKBKHircType.AudioDevice,
            _ => 0
        };
    }
}

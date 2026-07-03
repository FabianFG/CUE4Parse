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
    SidechainMix, // >= 168

    // Legacy hierarchies
    FeedbackBus = 0x80,
    FeedbackNode = 0x81
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
    public static string ToVersionString(this EAKBKHircType type, uint version)
    {
        return version switch
        {
            <= 125 => type.ToV125String(),
            _ => type.ToString()
        };
    }

    private static string ToV125String(this EAKBKHircType type)
    {
        return type switch
        {
            EAKBKHircType.State => EAKBKHircType_v125.Settings.ToString(),
            EAKBKHircType.SoundSfxVoice => EAKBKHircType_v125.SoundSfxVoice.ToString(),
            EAKBKHircType.EventAction => EAKBKHircType_v125.EventAction.ToString(),
            EAKBKHircType.Event => EAKBKHircType_v125.Event.ToString(),
            EAKBKHircType.RandomSequenceContainer => EAKBKHircType_v125.RandomSequenceContainer.ToString(),
            EAKBKHircType.SwitchContainer => EAKBKHircType_v125.SwitchContainer.ToString(),
            EAKBKHircType.ActorMixer => EAKBKHircType_v125.ActorMixer.ToString(),
            EAKBKHircType.AudioBus => EAKBKHircType_v125.AudioBus.ToString(),
            EAKBKHircType.LayerContainer => EAKBKHircType_v125.LayerContainer.ToString(),
            EAKBKHircType.MusicSegment => EAKBKHircType_v125.MusicSegment.ToString(),
            EAKBKHircType.MusicTrack => EAKBKHircType_v125.MusicTrack.ToString(),
            EAKBKHircType.MusicSwitchContainer => EAKBKHircType_v125.MusicSwitchContainer.ToString(),
            EAKBKHircType.MusicRandomSequenceContainer => EAKBKHircType_v125.MusicRandomSequenceContainer.ToString(),
            EAKBKHircType.Attenuation => EAKBKHircType_v125.Attenuation.ToString(),
            EAKBKHircType.DialogueEvent => EAKBKHircType_v125.DialogueEvent.ToString(),
            EAKBKHircType.FeedbackBus => EAKBKHircType_v125.FeedbackBus.ToString(),
            EAKBKHircType.FeedbackNode => EAKBKHircType_v125.FeedbackNode.ToString(),
            EAKBKHircType.FxShareSet => EAKBKHircType_v125.FxShareSet.ToString(),
            EAKBKHircType.FxCustom => EAKBKHircType_v125.FxCustom.ToString(),
            EAKBKHircType.AuxiliaryBus => EAKBKHircType_v125.AuxiliaryBus.ToString(),
            EAKBKHircType.LFO => EAKBKHircType_v125.LFO.ToString(),
            EAKBKHircType.Envelope => EAKBKHircType_v125.Envelope.ToString(),
            EAKBKHircType.AudioDevice => EAKBKHircType_v125.AudioDevice.ToString(),
            _ => type.ToString()
        };
    }

    public static EAKBKHircType MapToCurrent(this byte rawType, uint version)
    {
        if (version > 125)
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
            EAKBKHircType_v125.FeedbackBus => EAKBKHircType.FeedbackBus,
            EAKBKHircType_v125.FeedbackNode => EAKBKHircType.FeedbackNode,
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

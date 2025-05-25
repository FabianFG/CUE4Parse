namespace CUE4Parse.UE4.Wwise.Enums;

// Versions > 125, default
public enum EHierarchyObjectType : byte
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
    FxShareSet,
    FxCustom,
    AuxiliaryBus,
    LFO,
    Envelope,
    AudioDevice,
    TimeMod
}

// Versions <= 125
public enum EHierarchyObjectTypeV125 : byte
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

using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

[JsonConverter(typeof(HierarchyConverter))]
public readonly struct Hierarchy
{
    public readonly EHierarchyObjectType Type;
    public readonly uint Length;
    public readonly AbstractHierarchy Data;

    public Hierarchy(FArchive Ar)
    {
        byte rawType = Ar.Read<byte>();
        Length = Ar.Read<uint>();
        var hierarchyEndPosition = Ar.Position + Length;

        Type = MapHierarchyType(rawType);

        Data = WwiseVersions.IsSupported() ? Type switch
        {
            EHierarchyObjectType.Settings => new HierarchySettings(Ar),
            EHierarchyObjectType.SoundSfxVoice => new HierarchySoundSfxVoice(Ar),
            EHierarchyObjectType.EventAction => new HierarchyEventAction(Ar),
            EHierarchyObjectType.Event => new HierarchyEvent(Ar),
            EHierarchyObjectType.RandomSequenceContainer => new HierarchyRandomSequenceContainer(Ar),
            EHierarchyObjectType.SwitchContainer => new HierarchySwitchContainer(Ar),
            EHierarchyObjectType.ActorMixer => new HierarchyActorMixer(Ar),
            EHierarchyObjectType.AudioBus => new HierarchyAudioBus(Ar),
            EHierarchyObjectType.LayerContainer => new HierarchyLayerContainer(Ar),
            EHierarchyObjectType.MusicSegment => new HierarchyMusicSegment(Ar),
            EHierarchyObjectType.MusicTrack => new HierarchyMusicTrack(Ar),
            EHierarchyObjectType.MusicSwitchContainer => new HierarchyMusicSwitchContainer(Ar),
            EHierarchyObjectType.MusicRandomSequenceContainer => new HierarchyMusicRandomSequenceContainer(Ar),
            EHierarchyObjectType.Attenuation => new HierarchyAttenuation(Ar),
            EHierarchyObjectType.DialogueEvent => new HierarchyDialogueEvent(Ar),
            EHierarchyObjectType.FxShareSet => new HierarchyFxShareSet(Ar),
            EHierarchyObjectType.FxCustom => new HierarchyFxCustom(Ar),
            EHierarchyObjectType.AuxiliaryBus => new HierarchyAuxiliaryBus(Ar),
            EHierarchyObjectType.AudioDevice => new HierarchyAudioDevice(Ar),
            EHierarchyObjectType.LFO => new HierarchyLFO(Ar),
            EHierarchyObjectType.Envelope => new HierarchyEnvelope(Ar),
            EHierarchyObjectType.TimeMod => new HierarchyTimeMod(Ar),
            _ => new HierarchyGeneric(Ar)
        } : new HierarchyGeneric(Ar);

        if (Ar.Position != hierarchyEndPosition)
        {
#if DEBUG
            Log.Warning($"Didn't read hierarchy {Type} {Data.Id} correctly (at {Ar.Position}, should be {hierarchyEndPosition})");
            if (Data is HierarchyEventAction action)
            {
                Log.Warning($"EventAction type: {action.EventActionType}");
            }
#endif
            Ar.Position = hierarchyEndPosition;
        }
    }

    private static EHierarchyObjectType MapHierarchyType(byte rawType)
    {
        if (WwiseVersions.Version <= 125)
        {
            var typeV125 = (EHierarchyObjectTypeV125) rawType;

            return typeV125 switch
            {
                EHierarchyObjectTypeV125.Settings => EHierarchyObjectType.Settings,
                EHierarchyObjectTypeV125.SoundSfxVoice => EHierarchyObjectType.SoundSfxVoice,
                EHierarchyObjectTypeV125.EventAction => EHierarchyObjectType.EventAction,
                EHierarchyObjectTypeV125.Event => EHierarchyObjectType.Event,
                EHierarchyObjectTypeV125.RandomSequenceContainer => EHierarchyObjectType.RandomSequenceContainer,
                EHierarchyObjectTypeV125.SwitchContainer => EHierarchyObjectType.SwitchContainer,
                EHierarchyObjectTypeV125.ActorMixer => EHierarchyObjectType.ActorMixer,
                EHierarchyObjectTypeV125.AudioBus => EHierarchyObjectType.AudioBus,
                EHierarchyObjectTypeV125.LayerContainer => EHierarchyObjectType.LayerContainer,
                EHierarchyObjectTypeV125.MusicSegment => EHierarchyObjectType.MusicSegment,
                EHierarchyObjectTypeV125.MusicTrack => EHierarchyObjectType.MusicTrack,
                EHierarchyObjectTypeV125.MusicSwitchContainer => EHierarchyObjectType.MusicSwitchContainer,
                EHierarchyObjectTypeV125.MusicRandomSequenceContainer => EHierarchyObjectType.MusicRandomSequenceContainer,
                EHierarchyObjectTypeV125.Attenuation => EHierarchyObjectType.Attenuation,
                EHierarchyObjectTypeV125.DialogueEvent => EHierarchyObjectType.DialogueEvent,
                EHierarchyObjectTypeV125.FeedbackBus => 0,
                EHierarchyObjectTypeV125.FeedbackNode => 0,
                EHierarchyObjectTypeV125.FxShareSet => EHierarchyObjectType.FxShareSet,
                EHierarchyObjectTypeV125.FxCustom => EHierarchyObjectType.FxCustom,
                EHierarchyObjectTypeV125.AuxiliaryBus => EHierarchyObjectType.AuxiliaryBus,
                EHierarchyObjectTypeV125.LFO => EHierarchyObjectType.LFO,
                EHierarchyObjectTypeV125.Envelope => EHierarchyObjectType.Envelope,
                EHierarchyObjectTypeV125.AudioDevice => EHierarchyObjectType.AudioDevice,
                _ => 0,
            };
        }

        return (EHierarchyObjectType) rawType;
    }

}

public class HierarchyConverter : JsonConverter<Hierarchy>
{
    public override void WriteJson(JsonWriter writer, Hierarchy value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        writer.WriteValue(value.Type.ToString());

#if DEBUG
        writer.WritePropertyName("Length");
        writer.WriteValue(value.Length.ToString());
#endif

        writer.WritePropertyName("Id");
        writer.WriteValue(value.Data.Id.ToString());

        writer.WritePropertyName("Data");
        value.Data.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }

    public override Hierarchy ReadJson(JsonReader reader, Type objectType, Hierarchy existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

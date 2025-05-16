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
        Type = Ar.Read<EHierarchyObjectType>();
        Length = Ar.Read<uint>();
        var hierarchyEndPosition = Ar.Position + Length;
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
            EHierarchyObjectType.LFO => new HierarchyGeneric(Ar),
            EHierarchyObjectType.Envelope => new HierarchyGeneric(Ar),
            EHierarchyObjectType.TimeMod => new HierarchyGeneric(Ar),
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
}

public class HierarchyConverter : JsonConverter<Hierarchy>
{
    public override void WriteJson(JsonWriter writer, Hierarchy value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        writer.WriteValue(value.Type.ToString());

        // Helpful for debugging
        writer.WritePropertyName("Length");
        writer.WriteValue(value.Length.ToString());

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

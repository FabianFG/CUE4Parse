using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    [JsonConverter(typeof(HierarchyConverter))]
    public readonly struct Hierarchy
    {
        public readonly EHierarchyObjectType Type;
        public readonly int Length;
        public readonly AbstractHierarchy Data;

        public Hierarchy(FArchive Ar)
        {
            Type = Ar.Read<EHierarchyObjectType>();
            Length = Ar.Read<int>();
            var hierarchyEndPosition = Ar.Position + Length;
            Data = Type switch
            {
                EHierarchyObjectType.Settings => new HierarchySettings(Ar),
                //EHierarchyObjectType.Settings => new HierarchyGeneric(Ar, hierarchyEndPosition),
                EHierarchyObjectType.SoundSfxVoice => new HierarchySoundSfxVoice(Ar, hierarchyEndPosition),
                EHierarchyObjectType.EventAction => new HierarchyEventAction(Ar, hierarchyEndPosition),
                EHierarchyObjectType.Event => new HierarchyEvent(Ar),
                EHierarchyObjectType.RandomSequenceContainer => new HierarchyRandomSequenceContainer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.SwitchContainer => new HierarchySwitchContainer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.ActorMixer => new HierarchyActorMixer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.AudioBus => new HierarchyAudioBus(Ar, hierarchyEndPosition),
                EHierarchyObjectType.BlendContainer => new HierarchyBlendContainer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MusicSegment => new HierarchyMusicSegment(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MusicTrack => new HierarchyMusicTrack(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MusicSwitchContainer => new HierarchyMusicSwitchContainer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MusicPlaylistContainer => new HierarchyMusicPlaylistContainer(Ar, hierarchyEndPosition),
                EHierarchyObjectType.Attenuation => new HierarchyAttenuation(Ar, hierarchyEndPosition),
                EHierarchyObjectType.DialogueEvent => new HierarchyDialogueEvent(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MotionBus => new HierarchyMotionBus(Ar, hierarchyEndPosition),
                EHierarchyObjectType.MotionFx => new HierarchyMotionFx(Ar, hierarchyEndPosition),
                EHierarchyObjectType.Effect => new HierarchyEffect(Ar, hierarchyEndPosition),
                EHierarchyObjectType.AuxiliaryBus => new HierarchyAuxiliaryBus(Ar, hierarchyEndPosition),
                _ => new HierarchyGeneric(Ar, hierarchyEndPosition),
            };
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
}

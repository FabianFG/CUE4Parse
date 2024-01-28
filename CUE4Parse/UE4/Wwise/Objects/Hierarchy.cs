using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Serilog;

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
                // EHierarchyObjectType.Settings => new HierarchyGeneric(Ar),
                EHierarchyObjectType.SoundSfxVoice => new HierarchySoundSfxVoice(Ar),
                EHierarchyObjectType.EventAction => new HierarchyEventAction(Ar),
                EHierarchyObjectType.Event => new HierarchyEvent(Ar),
                EHierarchyObjectType.RandomSequenceContainer => new HierarchyRandomSequenceContainer(Ar),
                EHierarchyObjectType.SwitchContainer => new HierarchySwitchContainer(Ar),
                EHierarchyObjectType.ActorMixer => new HierarchyActorMixer(Ar),
                EHierarchyObjectType.AudioBus => new HierarchyAudioBus(Ar),
                EHierarchyObjectType.BlendContainer => new HierarchyBlendContainer(Ar),
                EHierarchyObjectType.MusicSegment => new HierarchyMusicSegment(Ar),
                EHierarchyObjectType.MusicTrack => new HierarchyMusicTrack(Ar),
                EHierarchyObjectType.MusicSwitchContainer => new HierarchyMusicSwitchContainer(Ar),
                EHierarchyObjectType.MusicPlaylistContainer => new HierarchyMusicPlaylistContainer(Ar),
                EHierarchyObjectType.Attenuation => new HierarchyAttenuation(Ar),
                EHierarchyObjectType.DialogueEvent => new HierarchyDialogueEvent(Ar),
                EHierarchyObjectType.MotionBus => new HierarchyMotionBus(Ar),
                EHierarchyObjectType.MotionFx => new HierarchyMotionFx(Ar),
                EHierarchyObjectType.Effect => new HierarchyEffect(Ar),
                EHierarchyObjectType.AuxiliaryBus => new HierarchyAuxiliaryBus(Ar),
                _ => new HierarchyGeneric(Ar),
            };

            if (Ar.Position != hierarchyEndPosition)
            {
#if DEBUG
                Log.Warning($"Didn't read hierarchy {Type} correctly (at {Ar.Position}, should be {hierarchyEndPosition})");
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
}

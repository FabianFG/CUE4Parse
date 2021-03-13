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
        public readonly AbstractHierarchy? Data;

        public Hierarchy(FArchive Ar)
        {
            Type = Ar.Read<EHierarchyObjectType>();
            Length = Ar.Read<int>();
            Ar.Position += Length; // delete this if you wanna read additional data
            Data = Type switch
            {
                // I've decided that i won't bother with this shit
                // EHierarchyObjectType.Settings => new HierarchySettings(Ar),
                // EHierarchyObjectType.SoundSfxVoice => new HierarchySoundSfxVoice(Ar),
                // EHierarchyObjectType.EventAction => new HierarchyEventAction(Ar),
                // EHierarchyObjectType.DialogueEvent => new HierarchyDialogueEvent(Ar),
                _ => null
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
            
            writer.WritePropertyName("Length");
            writer.WriteValue(value.Length);
            
            writer.WritePropertyName("Data");
            serializer.Serialize(writer, value.Data);
            
            writer.WriteEndObject();
        }

        public override Hierarchy ReadJson(JsonReader reader, Type objectType, Hierarchy existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
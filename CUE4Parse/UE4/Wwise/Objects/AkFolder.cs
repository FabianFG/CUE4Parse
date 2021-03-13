using System;
using System.Text;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    [JsonConverter(typeof(AkFolderConverter))]
    public class AkFolder
    {
        public readonly uint Offset;
        public readonly uint Id;
        public string Name;
        public AkEntry[] Entries;
        
        public AkFolder(FArchive Ar)
        {
            Offset = Ar.Read<uint>();
            Id = Ar.Read<uint>();
        }

        public void PopulateName(FArchive Ar)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var c = Ar.Read<char>();
                if (c == 0x00) break;
                sb.Append(c);
            }
            Name = sb.ToString().Trim();
        }
    }
    
    public class AkFolderConverter : JsonConverter<AkFolder>
    {
        public override void WriteJson(JsonWriter writer, AkFolder value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("Offset");
            writer.WriteValue(value.Offset);
            
            writer.WritePropertyName("Id");
            writer.WriteValue(value.Id);
            
            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            
            writer.WritePropertyName("Entries");
            serializer.Serialize(writer, value.Entries);
            
            writer.WriteEndObject();
        }

        public override AkFolder ReadJson(JsonReader reader, Type objectType, AkFolder existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
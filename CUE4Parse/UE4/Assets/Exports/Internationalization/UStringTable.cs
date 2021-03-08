using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization
{
    [JsonConverter(typeof(UStringTableConverter))]
    public class UStringTable : UObject
    {
        public FStringTable StringTable { get; private set; }
        public int StringTableId { get; private set; } // Index of the string in the NameMap

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            StringTable = new FStringTable(Ar);
            StringTableId = Ar.Read<int>();
        }
    }
    
    public class UStringTableConverter : JsonConverter<UStringTable>
    {
        public override void WriteJson(JsonWriter writer, UStringTable value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }
            
            // export properties
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("StringTable");
                serializer.Serialize(writer, value.StringTable);
                
                writer.WritePropertyName("StringTableId");
                writer.WriteValue(value.StringTableId);
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override UStringTable ReadJson(JsonReader reader, Type objectType, UStringTable existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

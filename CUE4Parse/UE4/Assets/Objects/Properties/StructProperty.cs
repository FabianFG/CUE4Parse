using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(StructPropertyConverter))]
    public class StructProperty : FPropertyTagType<UScriptStruct>
    {
        public StructProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            Value = new UScriptStruct(Ar, tagData?.StructType, type);
        }

        public override string ToString() => Value.ToString().SubstringBeforeLast(')') + ", StructProperty)";
    }
    
    public class StructPropertyConverter : JsonConverter<StructProperty>
    {
        public override void WriteJson(JsonWriter writer, StructProperty value, JsonSerializer serializer)
        {
            if (value.Value.StructType is FStructFallback sf)
            {
                writer.WriteStartObject();
                foreach (var property in sf.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
                writer.WriteEndObject();
            }
            else
            {
                serializer.Serialize(writer, value.Value.StructType);
            }
        }

        public override StructProperty ReadJson(JsonReader reader, Type objectType, StructProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
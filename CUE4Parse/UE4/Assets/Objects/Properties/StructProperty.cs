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
            Value = new UScriptStruct(Ar, tagData?.StructType, tagData?.Struct, type);
        }

        public override string ToString() => Value.ToString().SubstringBeforeLast(')') + ", StructProperty)";
    }

    public class StructPropertyConverter : JsonConverter<StructProperty>
    {
        public override void WriteJson(JsonWriter writer, StructProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override StructProperty ReadJson(JsonReader reader, Type objectType, StructProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
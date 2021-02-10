using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(AssetObjectPropertyConverter))]
    public class AssetObjectProperty : FPropertyTagType<string>
    {
        public AssetObjectProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => string.Empty,
                _ => Ar.ReadFString()
            };
        }
    }
    
    public class AssetObjectPropertyConverter : JsonConverter<AssetObjectProperty>
    {
        public override void WriteJson(JsonWriter writer, AssetObjectProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override AssetObjectProperty ReadJson(JsonReader reader, Type objectType, AssetObjectProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
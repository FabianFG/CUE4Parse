using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(LazyObjectPropertyConverter))]
    public class LazyObjectProperty : FPropertyTagType<FUniqueObjectGuid>
    {
        public LazyObjectProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FUniqueObjectGuid(),
                _ => Ar.Read<FUniqueObjectGuid>()
            };
        }
    }
    
    public class LazyObjectPropertyConverter : JsonConverter<LazyObjectProperty>
    {
        public override void WriteJson(JsonWriter writer, LazyObjectProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override LazyObjectProperty ReadJson(JsonReader reader, Type objectType, LazyObjectProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
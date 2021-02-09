using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(StrPropertyConverter))]
    public class StrProperty : FPropertyTagType<string>
    {
        public StrProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => string.Empty,
                _ => Ar.ReadFString()
            };
        }
    }

    public class StrPropertyConverter : JsonConverter<StrProperty>
    {
        public override void WriteJson(JsonWriter writer, StrProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override StrProperty ReadJson(JsonReader reader, Type objectType, StrProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
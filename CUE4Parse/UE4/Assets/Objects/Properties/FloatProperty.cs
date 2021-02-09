using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FloatPropertyConverter))]
    public class FloatProperty : FPropertyTagType<float>
    {
        public FloatProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<float>()
            };
        }
    }

    public class FloatPropertyConverter : JsonConverter<FloatProperty>
    {
        public override void WriteJson(JsonWriter writer, FloatProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override FloatProperty ReadJson(JsonReader reader, Type objectType, FloatProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
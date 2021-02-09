using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(DoublePropertyConverter))]
    public class DoubleProperty : FPropertyTagType<double>
    {
        public DoubleProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0.0,
                _ => Ar.Read<double>()
            };
        }
    }
    
    public class DoublePropertyConverter : JsonConverter<DoubleProperty>
    {
        public override void WriteJson(JsonWriter writer, DoubleProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override DoubleProperty ReadJson(JsonReader reader, Type objectType, DoubleProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
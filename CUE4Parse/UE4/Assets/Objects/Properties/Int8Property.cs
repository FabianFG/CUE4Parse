using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(Int8PropertyConverter))]
    public class Int8Property : FPropertyTagType<sbyte>
    {
        public Int8Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<sbyte>()
            };
        }
    }
    
    public class Int8PropertyConverter : JsonConverter<Int8Property>
    {
        public override void WriteJson(JsonWriter writer, Int8Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override Int8Property ReadJson(JsonReader reader, Type objectType, Int8Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(Int16PropertyConverter))]
    public class Int16Property : FPropertyTagType<short>
    {
        public Int16Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<short>()
            };
        }
    }
    
    public class Int16PropertyConverter : JsonConverter<Int16Property>
    {
        public override void WriteJson(JsonWriter writer, Int16Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override Int16Property ReadJson(JsonReader reader, Type objectType, Int16Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
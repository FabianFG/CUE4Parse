using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(Int64PropertyConverter))]
    public class Int64Property : FPropertyTagType<long>
    {
        public Int64Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<long>()
            };
        }
    }
    
    public class Int64PropertyConverter : JsonConverter<Int64Property>
    {
        public override void WriteJson(JsonWriter writer, Int64Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override Int64Property ReadJson(JsonReader reader, Type objectType, Int64Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UInt32PropertyConverter))]
    public class UInt32Property : FPropertyTagType<uint>
    {
        public UInt32Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<uint>()
            };
        }
    }
    
    public class UInt32PropertyConverter : JsonConverter<UInt32Property>
    {
        public override void WriteJson(JsonWriter writer, UInt32Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override UInt32Property ReadJson(JsonReader reader, Type objectType, UInt32Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
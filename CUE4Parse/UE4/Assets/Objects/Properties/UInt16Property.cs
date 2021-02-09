using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UInt16PropertyConverter))]
    public class UInt16Property : FPropertyTagType<ushort>
    {
        public UInt16Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<ushort>()
            };
        }
    }
    
    public class UInt16PropertyConverter : JsonConverter<UInt16Property>
    {
        public override void WriteJson(JsonWriter writer, UInt16Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override UInt16Property ReadJson(JsonReader reader, Type objectType, UInt16Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
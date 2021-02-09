using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UInt64PropertyConverter))]
    public class UInt64Property : FPropertyTagType<ulong>
    {
        public UInt64Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<ulong>()
            };
        }
    }
    
    public class UInt64PropertyConverter : JsonConverter<UInt64Property>
    {
        public override void WriteJson(JsonWriter writer, UInt64Property value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override UInt64Property ReadJson(JsonReader reader, Type objectType, UInt64Property existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(IntPropertyConverter))]
    public class IntProperty : FPropertyTagType<int>
    {
        public IntProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<int>()
            };
        }
    }

    public class IntPropertyConverter : JsonConverter<IntProperty>
    {
        public override void WriteJson(JsonWriter writer, IntProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override IntProperty ReadJson(JsonReader reader, Type objectType, IntProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
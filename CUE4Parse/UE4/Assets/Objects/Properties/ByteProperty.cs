using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(BytePropertyConverter))]
    public class ByteProperty : FPropertyTagType<byte>
    {
        public ByteProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                ReadType.NORMAL => Ar.Read<byte>(),
                ReadType.MAP => (byte) Ar.Read<uint>(),
                ReadType.ARRAY => Ar.Read<byte>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    public class BytePropertyConverter : JsonConverter<ByteProperty>
    {
        public override void WriteJson(JsonWriter writer, ByteProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override ByteProperty ReadJson(JsonReader reader, Type objectType, ByteProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Objects.RenderCore
{
    [JsonConverter(typeof(FPackedNormalConverter))]
    public class FPackedNormal
    {
        public readonly uint Data;
        public float X => (Data & 0xFF) / (float) 127.5 - 1;
        public float Y => ((Data >> 8) & 0xFF) / (float) 127.5 - 1;
        public float Z => ((Data >> 16) & 0xFF) / (float) 127.5 - 1;
        public float W => ((Data >> 24) & 0xFF) / (float) 127.5 - 1;

        public FPackedNormal(FArchive Ar)
        {
            Data = Ar.Read<uint>();
            if (Ar.Game >= EGame.GAME_UE4_20)
                Data ^= 0x80808080; 
        }

        public FPackedNormal(uint data)
        {
            Data = data;
        }

        public FPackedNormal(FVector Vector)
        {
            Data = (uint) ((int)(Vector.X + 1 * 127.5) + (int)(Vector.Y + 1 * 127.5) << 8 + (int)(Vector.Z + 1 * 127.5)) << 16;
        }

        public static explicit operator FVector(FPackedNormal packedNormal)
        {
            return new (packedNormal.X, packedNormal.Y, packedNormal.Z);
        }

        public static explicit operator FVector4(FPackedNormal packedNormal)
        {
            return new (packedNormal.X, packedNormal.Y, packedNormal.Z, packedNormal.W);
        }
    }

    public class FPackedNormalConverter : JsonConverter<FPackedNormal>
    {
        public override void WriteJson(JsonWriter writer, FPackedNormal value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Data");
            writer.WriteValue(value.Data);

            writer.WriteEndObject();
        }

        public override FPackedNormal ReadJson(JsonReader reader, Type objectType, FPackedNormal existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RenderCore
{
    [JsonConverter(typeof(FPackedNormalConverter))]
    public class FPackedNormal
    {
        public uint Data;
        public float X => (Data & 0xFF) / (float) 127.5 - 1;
        public float Y => ((Data >> 8) & 0xFF) / (float) 127.5 - 1;
        public float Z => ((Data >> 16) & 0xFF) / (float) 127.5 - 1;
        public float W => ((Data >> 24) & 0xFF) / (float) 127.5 - 1;

        public FPackedNormal(FArchive Ar)
        {
            Data = Ar.Read<uint>();
            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.IncreaseNormalPrecision)
                Data ^= 0x80808080;
        }

        public FPackedNormal(uint data)
        {
            Data = data;
        }

        public FPackedNormal(FVector vector)
        {
            Data = (uint) ((int) (vector.X + 1 * 127.5) + (int) (vector.Y + 1 * 127.5) << 8 + (int) (vector.Z + 1 * 127.5) << 16);
        }

        public FPackedNormal(FVector4 vector)// is this broken?
        {
            Data = (uint) ((int) (vector.X + 1 * 127.5) + (int) (vector.Y + 1 * 127.5) << 8 + (int) (vector.Z + 1 * 127.5) << 16 + (int) (vector.W + 1 * 127.5) << 24);
        }

        public void SetW(float value)
        {
            Data = (Data & 0xFFFFFF) | (uint) ((int) Math.Round(value * 127.0f) << 24);
        }

        public float GetW()
        {
            return (byte) (Data >> 24) / 127.0f;
        }

        public static explicit operator FVector(FPackedNormal packedNormal) => new(packedNormal.X, packedNormal.Y, packedNormal.Z);
        public static implicit operator FVector4(FPackedNormal packedNormal) => new(packedNormal.X, packedNormal.Y, packedNormal.Z, packedNormal.W);
        public static explicit operator Vector3(FPackedNormal packedNormal) => new(packedNormal.X, packedNormal.Y, packedNormal.Z);
        public static implicit operator Vector4(FPackedNormal packedNormal) => new(packedNormal.X, packedNormal.Y, packedNormal.Z, packedNormal.W);

        public static bool operator ==(FPackedNormal a, FPackedNormal b) => a.Data == b.Data && a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
        public static bool operator !=(FPackedNormal a, FPackedNormal b) => a.Data != b.Data || a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;
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

    public struct FDeprecatedSerializedPackedNormal
    {
        public uint Data;

        public static FVector4 VectorMultiplyAdd(FVector4 vec1, FVector4 vec2, FVector4 vec3) =>
            new(vec1.X * vec2.X + vec3.X, vec1.Y * vec2.Y + vec3.Y, vec1.Z * vec2.Z + vec3.Z, vec1.W * vec2.W + vec3.W);

        public static explicit operator FVector4(FDeprecatedSerializedPackedNormal packed)
        {
            var vectorToUnpack = new FVector4(packed.Data & 0xFF, (packed.Data >> 8) & 0xFF, (packed.Data >> 16) & 0xFF, (packed.Data >> 24) & 0xFF);
            return VectorMultiplyAdd(vectorToUnpack, new FVector4(1.0f / 127.5f), new FVector4(-1.0f));
        }

        public static explicit operator FVector(FDeprecatedSerializedPackedNormal packed) => (FVector) (FVector4) packed;
    }
}

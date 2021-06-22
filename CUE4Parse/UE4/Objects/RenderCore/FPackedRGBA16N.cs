using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Objects.RenderCore
{
    [JsonConverter(typeof(FPackedRGBA16NConverter))]
    public class FPackedRGBA16N
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Z;
        public readonly uint W;

        public FPackedRGBA16N(FArchive Ar)
        {
            X = Ar.Read<UInt16>();
            Y = Ar.Read<UInt16>();
            Z = Ar.Read<UInt16>();
            W = Ar.Read<UInt16>();

            if (Ar.Game >= EGame.GAME_UE4_20) {
                X = X ^ 0x8000;
                Y = Y ^ 0x8000;
                Z = Z ^ 0x8000;
                W = W ^ 0x8000;
            }
        }

        public static explicit operator FVector(FPackedRGBA16N packedRGBA16N)
        {
            float X = (packedRGBA16N.X - (float)32767.5) / (float)32767.5;
            float Y = (packedRGBA16N.Y - (float)32767.5) / (float)32767.5;
            float Z = (packedRGBA16N.Z - (float)32767.5) / (float)32767.5;

            return new FVector(X, Y, Z);
        }

        public static explicit operator FVector4(FPackedRGBA16N packedRGBA16N)
        {
            float X = (packedRGBA16N.X - (float)32767.5) / (float)32767.5;
            float Y = (packedRGBA16N.Y - (float)32767.5) / (float)32767.5;
            float Z = (packedRGBA16N.Z - (float)32767.5) / (float)32767.5;
            float W = (packedRGBA16N.W - (float)32767.5) / (float)32767.5;

            return new FVector4(X, Y, Z, W);
        }

       public static explicit operator FPackedNormal(FPackedRGBA16N packedRGBA16N)
       {
            return new FPackedNormal((FVector)packedRGBA16N);
       }
    }

    public class FPackedRGBA16NConverter : JsonConverter<FPackedRGBA16N>
    {
        public override void WriteJson(JsonWriter writer, FPackedRGBA16N value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            
            writer.WritePropertyName("W");
            writer.WriteValue(value.X);

            writer.WriteEndObject();
        }

        public override FPackedRGBA16N ReadJson(JsonReader reader, Type objectType, FPackedRGBA16N existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

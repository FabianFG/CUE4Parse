using System;
using System.Collections;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FN.Objects
{
    public enum EFortConnectivityCubeFace
    {
        Front,
        Left,
        Back,
        Right,
        Upper,
        Lower,
        MAX
    }

    [JsonConverter(typeof(FConnectivityCubeConverter))]
    public class FConnectivityCube : IUStruct
    {
        public readonly BitArray[] Faces = new BitArray[(int) EFortConnectivityCubeFace.MAX];

        public FConnectivityCube(FArchive Ar)
        {
            for (int i = 0; i < Faces.Length; i++)
            {
                // Reference: FArchive& operator<<(FArchive&, TBitArray&)
                var numBits = Ar.Read<int>();
                var numWords = numBits.DivideAndRoundUp(32);
                var data = Ar.ReadArray<int>(numWords);
                Faces[i] = new BitArray(data) { Length = numBits };
            }
        }
    }

    public class FConnectivityCubeConverter : JsonConverter<FConnectivityCube>
    {
        public override void WriteJson(JsonWriter writer, FConnectivityCube value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            for (int i = 0; i < value.Faces.Length; i++)
            {
                var face = value.Faces[i];
                writer.WritePropertyName(((EFortConnectivityCubeFace) i).ToString());
                writer.WriteStartArray();
                for (int j = 0; j < face.Length; j++)
                {
                    writer.WriteValue(face[j]);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public override FConnectivityCube ReadJson(JsonReader reader, Type objectType, FConnectivityCube existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

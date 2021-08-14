using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Meshes
{
    [JsonConverter(typeof(FPositionVertexBufferConverter))]
    public class FPositionVertexBuffer
    {
        public readonly FVector[] Verts;
        public readonly int Stride;
        public readonly int NumVertices;

        public FPositionVertexBuffer(FArchive Ar)
        {
            Stride = Ar.Read<int>();
            NumVertices = Ar.Read<int>();
            Verts = Ar.ReadBulkArray<FVector>();
        }
    }

    public class FPositionVertexBufferConverter : JsonConverter<FPositionVertexBuffer>
    {
        public override void WriteJson(JsonWriter writer, FPositionVertexBuffer value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Verts");
            serializer.Serialize(writer, value.Verts);

            writer.WritePropertyName("Stride");
            writer.WriteValue(value.Stride);

            writer.WritePropertyName("NumVertices");
            writer.WriteValue(value.NumVertices);

            writer.WriteEndObject();
        }

        public override FPositionVertexBuffer ReadJson(JsonReader reader, Type objectType, FPositionVertexBuffer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
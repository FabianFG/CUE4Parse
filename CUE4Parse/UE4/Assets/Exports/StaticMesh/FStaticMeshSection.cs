using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshSectionConverter))]
    public class FStaticMeshSection
    {
        public readonly int MaterialIndex;
        public readonly int FirstIndex;
        public readonly int NumTriangles;
        public readonly int MinVertexIndex;
        public readonly int MaxVertexIndex;
        public readonly bool bEnableCollision;
        public readonly bool bCastShadow;
        public readonly bool bForceOpaque;
        public readonly bool bVisibleInRayTracing;

        public FStaticMeshSection(FArchive Ar)
        {
            MaterialIndex = Ar.Read<int>();
            FirstIndex = Ar.Read<int>();
            NumTriangles = Ar.Read<int>();
            MinVertexIndex = Ar.Read<int>();
            MaxVertexIndex = Ar.Read<int>();
            bEnableCollision = Ar.ReadBoolean();
            bCastShadow = Ar.ReadBoolean();
            bForceOpaque = FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StaticMeshSectionForceOpaqueField && Ar.ReadBoolean();
            bVisibleInRayTracing = Ar.Versions["StaticMesh.HasVisibleInRayTracing"] && Ar.ReadBoolean();
            if (Ar.Game == EGame.GAME_RogueCompany) Ar.Position += 4;
        }
    }

    public class FStaticMeshSectionConverter : JsonConverter<FStaticMeshSection>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshSection value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("MaterialIndex");
            writer.WriteValue(value.MaterialIndex);

            writer.WritePropertyName("FirstIndex");
            writer.WriteValue(value.FirstIndex);

            writer.WritePropertyName("NumTriangles");
            writer.WriteValue(value.NumTriangles);

            writer.WritePropertyName("MinVertexIndex");
            writer.WriteValue(value.MinVertexIndex);

            writer.WritePropertyName("MaxVertexIndex");
            writer.WriteValue(value.MaxVertexIndex);

            writer.WritePropertyName("bEnableCollision");
            writer.WriteValue(value.bEnableCollision);

            writer.WritePropertyName("bCastShadow");
            writer.WriteValue(value.bCastShadow);

            writer.WritePropertyName("bForceOpaque");
            writer.WriteValue(value.bForceOpaque);

            writer.WritePropertyName("bVisibleInRayTracing");
            writer.WriteValue(value.bVisibleInRayTracing);

            writer.WriteEndObject();
        }

        public override FStaticMeshSection ReadJson(JsonReader reader, Type objectType, FStaticMeshSection existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
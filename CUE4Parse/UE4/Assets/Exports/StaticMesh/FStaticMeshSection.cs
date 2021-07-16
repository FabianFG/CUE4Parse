using System;
using CUE4Parse.UE4.Assets.Readers;
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
        public readonly bool EnableCollision;
        public readonly bool CastShadow;
        public readonly bool ForceOpaque;
        public readonly bool VisibleInRayTracing;

        public FStaticMeshSection(FAssetArchive Ar)
        {
            MaterialIndex = Ar.Read<int>();
            FirstIndex = Ar.Read<int>();
            NumTriangles = Ar.Read<int>();
            MinVertexIndex = Ar.Read<int>();
            MaxVertexIndex = Ar.Read<int>();
            EnableCollision = Ar.ReadBoolean();
            CastShadow = Ar.ReadBoolean();
            ForceOpaque = FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StaticMeshSectionForceOpaqueField && Ar.ReadBoolean();
            VisibleInRayTracing = Ar.Game >= EGame.GAME_UE4_26 && Ar.ReadBoolean();
            if (Ar.Game == EGame.GAME_ROGUECOMPANY) Ar.Position += 4;
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

            writer.WritePropertyName("EnableCollision");
            writer.WriteValue(value.EnableCollision);

            writer.WritePropertyName("CastShadow");
            writer.WriteValue(value.CastShadow);

            writer.WritePropertyName("ForceOpaque");
            writer.WriteValue(value.ForceOpaque);

            writer.WritePropertyName("VisibleInRayTracing");
            writer.WriteValue(value.VisibleInRayTracing);

            writer.WriteEndObject();
        }

        public override FStaticMeshSection ReadJson(JsonReader reader, Type objectType, FStaticMeshSection existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
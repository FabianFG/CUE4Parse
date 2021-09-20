using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Model;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UModel : Assets.Exports.UObject
    {
        public FBoxSphereBounds Bounds;
        public FVector[] Vectors;
        public FVector[] Points;
        public FBspNode[] Nodes;
        public FBspSurf[] Surfs;
        public FVert[] Verts;
        public int NumSharedSides;
        public bool RootOutside;
        public bool Linked;
        public uint NumUniqueVertices;
        public FModelVertexBuffer VertexBuffer;
        public FGuid LightingGuid;
        public FLightmassPrimitiveSettings[] LightmassSettings;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            var stripData = new FStripDataFlags(Ar);

            Bounds = Ar.Read<FBoxSphereBounds>();

            Vectors = Ar.ReadBulkArray<FVector>();
            Points = Ar.ReadBulkArray<FVector>();
            Nodes = Ar.ReadBulkArray<FBspNode>();

            Surfs = Ar.ReadArray(() => new FBspSurf(Ar));
            Verts = Ar.ReadBulkArray<FVert>();

            NumSharedSides = Ar.Read<int>();

            RootOutside = Ar.ReadBoolean();
            Linked = Ar.ReadBoolean();

            NumUniqueVertices = Ar.Read<uint>();

            if (!stripData.IsEditorDataStripped() || !stripData.IsClassDataStripped(1))
            {
                VertexBuffer = new FModelVertexBuffer(Ar);
            }

            LightingGuid = Ar.Read<FGuid>();
            LightmassSettings = Ar.ReadArray<FLightmassPrimitiveSettings>();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Bounds");
            serializer.Serialize(writer, Bounds);

            writer.WritePropertyName("Vectors");
            serializer.Serialize(writer, Vectors);

            writer.WritePropertyName("Points");
            serializer.Serialize(writer, Points);

            writer.WritePropertyName("Nodes");
            serializer.Serialize(writer, Nodes);

            writer.WritePropertyName("Surfs");
            serializer.Serialize(writer, Surfs);

            writer.WritePropertyName("NumSharedSides");
            serializer.Serialize(writer, NumSharedSides);

            writer.WritePropertyName("VertexBuffer");
            serializer.Serialize(writer, VertexBuffer);

            writer.WritePropertyName("LightingGuid");
            serializer.Serialize(writer, LightingGuid);

            writer.WritePropertyName("LightmassSettings");
            serializer.Serialize(writer, LightmassSettings);
        }
    }
}
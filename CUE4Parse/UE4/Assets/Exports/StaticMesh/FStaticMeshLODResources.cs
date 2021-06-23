using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshLODResourcesConverter))]
    public class FStaticMeshLODResources
    {
        public FStaticMeshSection[] Sections { get; private set; }
        public float MaxDeviation { get; private set; }
        public FPositionVertexBuffer? PositionVertexBuffer { get; private set; }
        public FStaticMeshVertexBuffer? VertexBuffer { get; private set; }
        public FColorVertexBuffer? ColorVertexBuffer { get; private set; }
        public FRawStaticIndexBuffer? IndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? ReversedIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? DepthOnlyIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? ReversedDepthOnlyIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? WireframeIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? AdjacencyIndexBuffer { get; private set; }

        public enum EClassDataStripFlag : byte
        {
            CDSF_AdjacencyData = 1,
            CDSF_MinLodData = 2,
            CDSF_ReversedIndexBuffer = 4,
            CDSF_RayTracingResources = 8
        }

        public FStaticMeshLODResources(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();

            Sections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
            MaxDeviation = Ar.Read<float>();

            if (Ar.Game < EGame.GAME_UE4_23)
            {
                if (!stripDataFlags.IsDataStrippedForServer() && !stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_MinLodData))
                {
                    SerializeBuffersLegacy(Ar, stripDataFlags);
                }
                return;
            }

            bool bIsLODCookedOut, bInlined;
            bIsLODCookedOut = Ar.ReadBoolean();
            bInlined = Ar.ReadBoolean();

            if (!stripDataFlags.IsDataStrippedForServer() && !bIsLODCookedOut)
            {
                if (bInlined) SerializeBuffers(Ar);
                else
                {
                    var BulkData = new FByteBulkData(Ar);
                    if (BulkData.Header.ElementCount > 0)
                    {
                        var tempAr = new FAssetArchive(new FByteArchive("FStaticMeshBufferReader", BulkData.Data, Ar.Game, Ar.Ver), Ar.Owner);
                        SerializeBuffers(tempAr);
                    }

                    uint depthOnlyNumTriangles = Ar.Read<uint>();
                    uint packedData = Ar.Read<uint>();

                    Ar.Position += 4 * 4 + 2 * 4 + 2 * 4 + 6 * (2 * 4);
                                // StaticMeshVertexBuffer = 2x int32, 2x bool
                                // PositionVertexBuffer = 2x int32
                                // ColorVertexBuffer = 2x int32
                                // IndexBuffer = int32 + bool
                                // ReversedIndexBuffer
                                // DepthOnlyIndexBuffer
                                // ReversedDepthOnlyIndexBuffer
                                // WireframeIndexBuffer
                                // AdjacencyIndexBuffer
                }
            }

            // FStaticMeshBuffersSize
            uint serializedBuffersSize = Ar.Read<uint>();
            uint depthOnlyIBSize = Ar.Read<uint>();
            uint reversedIBsSize = Ar.Read<uint>();
        }

        // Pre-UE4.23 code
        public void SerializeBuffersLegacy(FArchive Ar, FStripDataFlags stripDataFlags)
        {
            throw new NotImplementedException();
        }

        public void SerializeBuffers(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();

            this.PositionVertexBuffer = new FPositionVertexBuffer(Ar);
            this.VertexBuffer = new FStaticMeshVertexBuffer(Ar);
            this.ColorVertexBuffer = new FColorVertexBuffer(Ar);
            this.IndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_ReversedIndexBuffer))
            {
                this.ReversedIndexBuffer = new FRawStaticIndexBuffer(Ar); ;
            }

            this.DepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_ReversedIndexBuffer))
                this.ReversedDepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsEditorDataStripped())
                this.WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                this.AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (Ar.Game >= EGame.GAME_UE4_25 & !stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_RayTracingResources))
                Ar.ReadBulkArray(() => Ar.ReadByte());

            Enumerable.Repeat(new FWeightedRandomSampler(Ar), Sections.Length); // FStaticMeshSectionAreaWeightedTriangleSampler
            new FWeightedRandomSampler(Ar);  // FStaticMeshAreaWeightedSectionSample
        }
    }

    public class FStaticMeshLODResourcesConverter : JsonConverter<FStaticMeshLODResources>
    {
        public override void WriteJson(JsonWriter writer, FStaticMeshLODResources value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Sections");
            serializer.Serialize(writer, value.Sections);

            writer.WritePropertyName("MaxDeviation");
            writer.WriteValue(value.MaxDeviation);

            writer.WriteEndObject();
        }

        public override FStaticMeshLODResources ReadJson(JsonReader reader, Type objectType, FStaticMeshLODResources existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
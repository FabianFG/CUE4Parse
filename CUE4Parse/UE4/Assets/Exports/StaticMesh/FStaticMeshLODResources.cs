using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [JsonConverter(typeof(FStaticMeshLODResourcesConverter))]
    public class FStaticMeshLODResources
    {
        public FStaticMeshSection[] Sections { get; }
        public FCardRepresentationData? CardRepresentationData { get; set; }
        public float MaxDeviation { get; }
        public FPositionVertexBuffer? PositionVertexBuffer { get; private set; }
        public FStaticMeshVertexBuffer? VertexBuffer { get; private set; }
        public FColorVertexBuffer? ColorVertexBuffer { get; private set; }
        public FRawStaticIndexBuffer? IndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? ReversedIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? DepthOnlyIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? ReversedDepthOnlyIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? WireframeIndexBuffer { get; private set; }
        public FRawStaticIndexBuffer? AdjacencyIndexBuffer { get; private set; }
        public bool SkipLod => VertexBuffer == null || IndexBuffer == null ||
                               PositionVertexBuffer == null || ColorVertexBuffer == null;

        public enum EClassDataStripFlag : byte
        {
            CDSF_AdjacencyData = 1,
            CDSF_MinLodData = 2,
            CDSF_ReversedIndexBuffer = 4,
            CDSF_RayTracingResources = 8,

            // PUBG all 3 bits set, no idea what indicates what, they're just always set.
            CDSF_StripIndexBuffers = 128 | 64 | 32
        }

        public FStaticMeshLODResources(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();

            if (Ar.Game == EGame.GAME_TheDivisionResurgence) Ar.Position += 4;

            Sections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
            MaxDeviation = Ar.Read<float>();

            if (!Ar.Versions["StaticMesh.UseNewCookedFormat"])
            {
                if (!stripDataFlags.IsDataStrippedForServer() && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_MinLodData))
                {
                    SerializeBuffersLegacy(Ar, stripDataFlags);
                }

                return;
            }

            var bIsLODCookedOut = false;
            if (Ar.Game != EGame.GAME_Splitgate)
                bIsLODCookedOut = Ar.ReadBoolean();
            var bInlined = Ar.ReadBoolean() || Ar.Game == EGame.GAME_RogueCompany;

            if (!stripDataFlags.IsDataStrippedForServer() && !bIsLODCookedOut)
            {
                if (bInlined)
                {
                    SerializeBuffers(Ar);
                    switch (Ar.Game)
                    {
                        case EGame.GAME_RogueCompany:
                            Ar.Position += 10;
                            break;
                        case EGame.GAME_TheDivisionResurgence:
                            Ar.Position += 12;
                            break;
                    }
                }
                else
                {
                    var bulkData = new FByteBulkData(Ar);
                    if (bulkData.Header.ElementCount > 0)
                    {
                        var tempAr = new FByteArchive("StaticMeshBufferReader", bulkData.Data, Ar.Versions);
                        SerializeBuffers(tempAr);
                        tempAr.Dispose();
                    }

                    // https://github.com/EpicGames/UnrealEngine/blob/4.27/Engine/Source/Runtime/Engine/Private/StaticMesh.cpp#L560
                    Ar.Position += 8; // DepthOnlyNumTriangles + Packed
                    Ar.Position += 4 * 4 + 2 * 4 + 2 * 4 + 5 * 2 * 4;
                                // StaticMeshVertexBuffer = 2x int32, 2x bool
                                // PositionVertexBuffer = 2x int32
                                // ColorVertexBuffer = 2x int32
                                // IndexBuffer = int32 + bool
                                // ReversedIndexBuffer
                                // DepthOnlyIndexBuffer
                                // ReversedDepthOnlyIndexBuffer
                                // WireframeIndexBuffer
                    if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RemovingTessellation)
                    {
                        Ar.Position += 2 * 4; // AdjacencyIndexBuffer
                    }
                    if (Ar.Game == EGame.GAME_StarWarsJediSurvivor) Ar.Position += 4; // bDropNormals
                }

                // FStaticMeshBuffersSize
                // uint32 SerializedBuffersSize = 0;
                // uint32 DepthOnlyIBSize       = 0;
                // uint32 ReversedIBsSize       = 0;
                Ar.Position += 12;

                if (Ar.Game == EGame.GAME_StarWarsJediSurvivor) Ar.Position += 4;
            }
        }

        // Pre-UE4.23 code
        public void SerializeBuffersLegacy(FAssetArchive Ar, FStripDataFlags stripDataFlags)
        {
            PositionVertexBuffer = new FPositionVertexBuffer(Ar);
            VertexBuffer = new FStaticMeshVertexBuffer(Ar);

            if (Ar.Game == EGame.GAME_Borderlands3)
            {
                var numColorStreams = Ar.Read<int>();
                if (numColorStreams != 0)
                {
                    ColorVertexBuffer = new FColorVertexBuffer(Ar);
                    for (var i = 0; i < numColorStreams - 1; i++)
                    {
                        var _ = new FColorVertexBuffer(Ar);
                    }
                }
            }
            else
            {
                ColorVertexBuffer = new FColorVertexBuffer(Ar);
            }

            IndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (Ar.Game != EGame.GAME_PlayerUnknownsBattlegrounds || !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_StripIndexBuffers))
            {
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.SOUND_CONCURRENCY_PACKAGE && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_ReversedIndexBuffer))
                {
                    ReversedIndexBuffer = new FRawStaticIndexBuffer(Ar);
                    DepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);
                    ReversedDepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);
                }
                else
                {
                    // UE4.8 or older, or when has CDSF_ReversedIndexBuffer
                    DepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);
                }

                if (Ar.Ver >= EUnrealEngineObjectUE4Version.FTEXT_HISTORY && Ar.Ver < EUnrealEngineObjectUE4Version.RENAME_CROUCHMOVESCHARACTERDOWN)
                {
                    var _ = new FDistanceFieldVolumeData(Ar); // distanceFieldData
                }

                if (!stripDataFlags.IsEditorDataStripped())
                    WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

                if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
                    AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);
            }

            if (Ar.Game > EGame.GAME_UE4_16)
            {
                for (var i = 0; i < Sections.Length; i++)
                {
                    var _ = new FWeightedRandomSampler(Ar);
                }

                _ = new FWeightedRandomSampler(Ar);
            }

            if (Ar.Game == EGame.GAME_SeaOfThieves) Ar.Position += 17;
        }

        public void SerializeBuffers(FArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();

            PositionVertexBuffer = new FPositionVertexBuffer(Ar);
            VertexBuffer = new FStaticMeshVertexBuffer(Ar);
            ColorVertexBuffer = new FColorVertexBuffer(Ar);

            if (Ar.Game == EGame.GAME_RogueCompany)
            {
                var _ = new FColorVertexBuffer(Ar);
            }

            IndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_ReversedIndexBuffer))
            {
                ReversedIndexBuffer = new FRawStaticIndexBuffer(Ar);
            }

            DepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_ReversedIndexBuffer))
                ReversedDepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsEditorDataStripped())
                WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RemovingTessellation && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
                AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (Ar.Versions["StaticMesh.HasRayTracingGeometry"] && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_RayTracingResources))
            {
                var _ = Ar.ReadBulkArray<byte>(); // rayTracingGeometry
            }

            // https://github.com/EpicGames/UnrealEngine/blob/4.27/Engine/Source/Runtime/Engine/Private/StaticMesh.cpp#L547
            var areaWeightedSectionSamplers = new FWeightedRandomSampler[Sections.Length];
            for (var i = 0; i < Sections.Length; i++)
            {
                areaWeightedSectionSamplers[i] = new FWeightedRandomSampler(Ar);
            }

            _ = new FWeightedRandomSampler(Ar); // areaWeightedSampler
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

            writer.WritePropertyName("PositionVertexBuffer");
            serializer.Serialize(writer, value.PositionVertexBuffer);

            writer.WritePropertyName("VertexBuffer");
            serializer.Serialize(writer, value.VertexBuffer);

            writer.WritePropertyName("ColorVertexBuffer");
            serializer.Serialize(writer, value.ColorVertexBuffer);

            if (value.CardRepresentationData != null)
            {
                writer.WritePropertyName("CardRepresentationData");
                serializer.Serialize(writer, value.CardRepresentationData);
            }

            writer.WriteEndObject();
        }

        public override FStaticMeshLODResources ReadJson(JsonReader reader, Type objectType, FStaticMeshLODResources existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

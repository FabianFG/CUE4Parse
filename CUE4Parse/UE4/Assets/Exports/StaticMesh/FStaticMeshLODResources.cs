using CUE4Parse.GameTypes.FF7.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

[JsonConverter(typeof(FStaticMeshLODResourcesConverter))]
public class FStaticMeshLODResources
{
    public FStaticMeshSection[] Sections { get; }
    public FBoxSphereBounds? SourceMeshBounds;
    public FCardRepresentationData? CardRepresentationData { get; set; }
    public float MaxDeviation { get; }
    public FPositionVertexBuffer? PositionVertexBuffer { get; set; }
    public FStaticMeshVertexBuffer? VertexBuffer { get; private set; }
    public FColorVertexBuffer? ColorVertexBuffer { get; set; }
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

    public FStaticMeshLODResources(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar);

        if (Ar.Game == GAME_TheDivisionResurgence) Ar.Position += 4;

        Sections = Ar.ReadArray(() => new FStaticMeshSection(Ar));

        if (Ar.Game >= GAME_UE5_6)
        {
            SourceMeshBounds = new FBoxSphereBounds(Ar);
        }
        if (Ar.Game >= GAME_UE4_0)
        {
            MaxDeviation = Ar.Read<float>();
        }

        if (Ar.Game is GAME_ThePathless or GAME_ARKSurvivalAscended) Ar.Position += 4;
        if (Ar.Game == GAME_NeedForSpeedMobile)
        {
            Ar.SkipFixedArray(36);
            Ar.SkipFixedArray(28);
            Ar.Position += 8;
        }

        if (!Ar.Versions["StaticMesh.UseNewCookedFormat"])
        {
            if (!stripDataFlags.IsAudioVisualDataStripped() && !stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_MinLodData))
            {
                SerializeBuffersLegacy(Ar, stripDataFlags);
            }

            return;
        }

        if (Ar.Game is GAME_ArenaBreakoutMobile) Ar.SkipFixedArray(28);

        var bIsLODCookedOut = false;
        if (Ar.Game != GAME_Splitgate)
            bIsLODCookedOut = Ar.ReadBoolean();
        var bInlined = Ar.ReadBoolean() || Ar.Game == GAME_RogueCompany;

        if (Ar.Game is GAME_LordOfMysteries) Ar.Position += 4;

        if (!stripDataFlags.IsAudioVisualDataStripped() && !bIsLODCookedOut)
        {
            if (Ar.Game >= GAME_UE5_5 || Ar.Game == GAME_MetalGearSolidDelta) Ar.Position += 4; // bHasRayTracingGeometry

            if (bInlined)
            {
                SerializeBuffers(Ar);
                switch (Ar.Game)
                {
                    case GAME_RogueCompany:
                        Ar.Position += 10;
                        break;
                    case GAME_TheDivisionResurgence:
                        Ar.Position += 12;
                        break;
                    case GAME_PUBGBlackBudget:
                        Ar.Position += 4;
                        if (Ar.Read<int>() > 0) Ar.SkipBulkArrayData();
                        break;
                    case GAME_HonorofKingsWorld:
                        _ = Ar.ReadArray(2, () => new FRawStaticIndexBuffer(Ar));
                        Ar.Position += 4;
                        var additionalBuffers = Ar.ReadArray(4, () => new FRawStaticIndexBuffer(Ar));
                        if (additionalBuffers[0] is { Buffer: {Length: > 0 }}) Sections[0].CustomData = 1; // flag for custom serialization in FStaticMeshRenderData
                        break;
                    case GAME_InfinityNikki when Sections.Any(x => x.CustomData is 1):
                        _ = Ar.ReadArray(4, () => new FRawStaticIndexBuffer(Ar));
                        break;
                }
            }
            else if (Ar is FAssetArchive assetArchive)
            {
                var bulkData = new FByteBulkData(assetArchive);
                if (bulkData.Header.ElementCount > 0 && bulkData.Data != null)
                {
                    using var tempAr = new FByteArchive("StaticMeshBufferReader", bulkData.Data, Ar.Versions);
                    SerializeBuffers(tempAr);
                }

                if (Ar.Game is GAME_HonorofKingsWorld)
                {
                    Ar.Position += 8;
                    Ar.Position += Ar.Read<int>() == 32 ? 64 : 40;
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

                Ar.Position += Ar.Game switch
                {
                    >= GAME_UE5_6 => 6 * 4, // RawDataHeader = 6x uint32
                    GAME_ArenaBreakoutMobile => 44,
                    GAME_NeedForSpeedMobile => 32,
                    GAME_SuicideSquad => 29,
                    GAME_ArenaBreakoutInfinite => 16,
                    GAME_TheFinals or GAME_ArcRaiders => 12,
                    GAME_StarWarsJediSurvivor or GAME_DeltaForce => 4, // bDropNormals
                    GAME_FateTrigger => 5,
                    _ => 0
                };
            }

            // FStaticMeshBuffersSize
            // uint32 SerializedBuffersSize = 0;
            // uint32 DepthOnlyIBSize       = 0;
            // uint32 ReversedIBsSize       = 0;
            Ar.Position += 12;

            if (Ar.Game is GAME_StarWarsJediSurvivor or GAME_TheFinals or GAME_ArcRaiders) Ar.Position += 4;
            if (Ar.Game is GAME_NeedForSpeedMobile)
            {
                var count = Ar.Read<int>();
                for (var i = 0; i < count; i++)
                {
                    Ar.Position += 4;
                    Ar.SkipMultipleFixedArrays(2, 4);
                }
            }
        }
    }

    // Pre-UE4.23 code
    public void SerializeBuffersLegacy(FArchive Ar, FStripDataFlags stripDataFlags)
    {
        if (Ar.Game is GAME_Abzu) Ar.Position += 4;

        PositionVertexBuffer = new FPositionVertexBuffer(Ar);
        VertexBuffer = new FStaticMeshVertexBuffer(Ar);

        if (Ar.Game == GAME_Borderlands3)
        {
            var numColorStreams = Ar.Read<int>();
            if (numColorStreams != 0)
            {
                ColorVertexBuffer = new FColorVertexBuffer(Ar);
                for (var i = 0; i < numColorStreams - 1; i++)
                {
                    _ = new FColorVertexBuffer(Ar);
                }
            }
            else
            {
                ColorVertexBuffer = new FColorVertexBuffer();
            }
        }
        else
        {
            ColorVertexBuffer = new FColorVertexBuffer(Ar);
        }

        IndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (Ar.Game == GAME_NarutotoBorutoShinobiStriker)
        {
            if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
                AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);
            Ar.ReadArray(Sections.Length + 1, () => new FWeightedRandomSampler(Ar));
            return;
        }

        if (Ar.Game != GAME_PlayerUnknownsBattlegrounds || !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_StripIndexBuffers))
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

            if (Ar.Game is GAME_Abzu)
            {
                Ar.Position += 4;
                Ar.SkipMultipleFixedArrays([8, 4, 24, 4]);
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.FTEXT_HISTORY && Ar.Ver < EUnrealEngineObjectUE4Version.RENAME_CROUCHMOVESCHARACTERDOWN)
            {
                _ = new FDistanceFieldVolumeData(Ar); // distanceFieldData
            }

            if (!stripDataFlags.IsEditorDataStripped())
                WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

            if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
                AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);
        }

        if (Ar.Game > GAME_UE4_16)
        {
            for (var i = 0; i < Sections.Length; i++)
            {
                _ = new FWeightedRandomSampler(Ar);
            }

            _ = new FWeightedRandomSampler(Ar);
        }

        if (Ar.Game == GAME_SeaOfThieves) Ar.Position += 17;
    }

    public void SerializeBuffers(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar);

        PositionVertexBuffer = new FPositionVertexBuffer(Ar);
        VertexBuffer = new FStaticMeshVertexBuffer(Ar);
        ColorVertexBuffer = new FColorVertexBuffer(Ar);

        if (Ar.Game is GAME_RogueCompany)
        {
            _ = new FColorVertexBuffer(Ar);
        }
        if (Ar.Game == GAME_ThePathless) Ar.Position += 20;

        IndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_ReversedIndexBuffer))
        {
            ReversedIndexBuffer = new FRawStaticIndexBuffer(Ar);
        }

        if (Ar.Game is GAME_OutlastTrials) Ar.Position += 4;

        DepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_ReversedIndexBuffer))
            ReversedDepthOnlyIndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (!stripDataFlags.IsEditorDataStripped())
            WireframeIndexBuffer = new FRawStaticIndexBuffer(Ar);

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RemovingTessellation &&
            !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
        {
            if (Ar.Game != GAME_GTATheTrilogyDefinitiveEdition && Ar.Game != GAME_FinalFantasy7Rebirth)
                AdjacencyIndexBuffer = new FRawStaticIndexBuffer(Ar);
        }

        if (Ar.Game == GAME_OutlastTrials) Ar.Position += 4;

        if (Ar.Game is GAME_ArenaBreakoutInfinite)
        {
            _ = new FRawStaticIndexBuffer(Ar);
            _ = new FRawStaticIndexBuffer(Ar);
        }
        if (Ar.Game is GAME_ArenaBreakoutMobile)
        {
            Ar.SkipMultipleBulkArrayData(4);
        }
        if (Ar.Game is GAME_TheFinals or GAME_ArcRaiders)
        {
            _ = new FRawStaticIndexBuffer(Ar);
            Ar.Position += 4; // Vert count
        }

        if (Ar.Game == GAME_FinalFantasy7Rebirth)
        {
            if (FF7FStaticMeshLODResources.SerializeIndexBuffer(Ar, out var sections, out var indexBuffer))
            {
                var len = Math.Min(sections.Length, Sections.Length);
                for (var i = 0; i < len; i++)
                {
                    Sections[i].NumTriangles = sections[i];
                }
                IndexBuffer.SetIndices(indexBuffer);
            }
        }

        if (Ar.Versions["StaticMesh.HasRayTracingGeometry"] && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_RayTracingResources))
        {
            if (Ar.Game >= GAME_UE5_6)
                Ar.Position += 6 * sizeof(uint); // RawDataHeader = 6x uint32

            Ar.SkipBulkArrayData(); // rayTracingGeometry
        }

        // https://github.com/EpicGames/UnrealEngine/blob/4.27/Engine/Source/Runtime/Engine/Private/StaticMesh.cpp#L547
        _ = Ar.ReadArray(Sections.Length, () => new FWeightedRandomSampler(Ar)); // areaWeightedSectionSamplers
        _ = new FWeightedRandomSampler(Ar); // areaWeightedSampler
    }
}

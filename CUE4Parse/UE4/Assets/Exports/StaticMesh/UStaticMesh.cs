using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class UStaticMesh : UObject
{
    public bool bCooked { get; private set; }
    public bool HasTangents { get; private set; }
    public FPackageIndex BodySetup { get; private set; }
    public FPackageIndex NavCollision { get; private set; }
    public FGuid LightingGuid { get; private set; }
    public FPackageIndex[]? Sockets { get; private set; } // UStaticMeshSocket[]
    public FStaticMeshRenderData? RenderData { get; private set; }
    public FPackageIndex?[] Materials { get; private set; } = []; // UMaterialInterface[]
    public FStaticMaterial[] StaticMaterials { get; private set; } = [];
    public int LODForCollision { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 12;
        base.Deserialize(Ar, validPos);
        LODForCollision = GetOrDefault(nameof(LODForCollision), 0);

        var stripDataFlags = new FStripDataFlags(Ar);
        bCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_MESH_REFACTOR && Ar.ReadBoolean();
        HasTangents = Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_VERTEXBUFFER_MERGE;

        var Bounds = new FBoxSphereBounds();
        if (!stripDataFlags.IsEditorDataStripped() && Ar.Ver < EUnrealEngineObjectUE4Version.STATIC_MESH_REFACTOR)
        {
            Bounds = new FBoxSphereBounds(Ar);
        }

        if (Ar.Game == GAME_WutheringWaves && GetOrDefault<bool>("bUseStandaloneBodySetup"))
            BodySetup = GetOrDefault<FPackageIndex>("StandaloneBodySetup");
        else
            BodySetup = new FPackageIndex(Ar);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.REMOVE_STATICMESH_COLLISIONMODEL)
        {
            new FPackageIndex(Ar); // CollisionModel;
        }

        if (Ar.Versions["StaticMesh.HasNavCollision"])
            NavCollision = new FPackageIndex(Ar);

        if (Ar.Game < GAME_UE4_0)
        {
            if (Ar.Ver < EUnrealEngineObjectUE3Version.COMPACTKDOPSTATICMESH || Ar.Game == GAME_Dishonored)
            {
                // hacky way to skip without defining struct, can't use skipbulkarray because of EUnrealEngineObjectUE3Version.ADDED_BULKSERIALIZE_SANITY_CHECKING
                Ar.ReadBulkArray(() =>
                {
                    Ar.Position += 24 + 4;
                    if (Ar.Ver < EUnrealEngineObjectUE3Version.DeprecatedShortProperties || Ar.Ver > EUnrealEngineObjectUE3Version.CLEANUP_SOUNDNODEWAVE)
                        Ar.Position += 4;
                    else
                        Ar.Position += 8;
                    return (byte) 0;
                });
            }
            else
            {
                Ar.Position += 24;
                Ar.ReadBulkArray(() => Ar.ReadBytes(6)); // bound
            }

            if (Ar.Ver < EUnrealEngineObjectUE3Version.DeprecatedShortProperties || Ar.Ver > EUnrealEngineObjectUE3Version.CLEANUP_SOUNDNODEWAVE)
            {
                Ar.ReadBulkArray(() => Ar.ReadBytes(8)); // Collision Triangle
            }
            else
            {
                Ar.ReadBulkArray(() => Ar.ReadBytes(16)); // Collision Triangle
            }

            var InternalVersion = Ar.Read<int>();
            var STATICMESH_VERSION_CONTENT_TAGS = 17; // Content tags were introduced in SM version 17

            if (InternalVersion >= STATICMESH_VERSION_CONTENT_TAGS && Ar.Ver < EUnrealEngineObjectUE3Version.REMOVED_LEGACY_CONTENT_TAGS)
            {
                Ar.ReadArray(Ar.ReadFName); // ContentTags
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATIC_MESH_SOURCE_DATA_COPY)
            {
                var bHaveSourceData = Ar.ReadBoolean();
                if (bHaveSourceData)
                {
                    RenderData = new FStaticMeshRenderData { LODs = [new FStaticMeshLODResources(Ar)] };
                }

                if (Ar.Ver < EUnrealEngineObjectUE3Version.STORE_MESH_OPTIMIZATION_SETTINGS)
                {
                    Ar.SkipArray<int>(); // OptimizationSettings
                }
                else
                {
                    if (Ar.Ver < EUnrealEngineObjectUE3Version.ADDED_EXTRA_MESH_OPTIMIZATION_SETTINGS)
                    {
                        Ar.SkipFixedArray(7);
                    }
                    else
                    {
                        Ar.SkipFixedArray(24);
                    }
                }

                Ar.ReadBoolean(); // bHasBeenSimplified
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.TAG_MESH_PROXIES)
            {
                Ar.ReadBoolean(); // bIsMeshProxy
            }

            RenderData = new FStaticMeshRenderData(Ar);
            RenderData.Bounds = Bounds;

            Materials = new FPackageIndex[RenderData.LODs[0].Sections.Length];
            for (var i = 0; i < RenderData.LODs[0].Sections.Length; i++)
            {
                Materials[i] = RenderData.LODs[0].Sections[i].Material!;
            }

            Ar.Read<int>(); // LODInfo
        }

        if (!stripDataFlags.IsEditorDataStripped())
        {
            if (Ar.Ver < EUnrealEngineObjectUE4Version.DEPRECATED_STATIC_MESH_THUMBNAIL_PROPERTIES_REMOVED)
            {
                 var dummyThumbnailAngle = new FRotator(Ar);
                 if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_THUMBNAIL_DISTANCE)
                 {
                     var dummyThumbnailDistance = Ar.Read<float>();
                 }
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_VERSION_18 && FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.DeprecatedHighResSourceMesh && Ar.Game is not GAME_APBReloaded)
            {
                var Deprecated_HighResSourceMeshName = Ar.ReadFString();
                var Deprecated_HighResSourceMeshCRC = Ar.Read<uint>();
            }
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.INTEGRATED_LIGHTMASS)
        {
            LightingGuid = Ar.Read<FGuid>(); // LocalLightingGuid
        }
        else
        {
            LightingGuid = FGuid.Random();
        }

        if (Ar.Game == GAME_Dishonored)
        {
            Ar.Position = validPos;
            return; // some weird changes so just ignore
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRESERVE_SMC_VERT_COLORS && Ar.Ver < EUnrealEngineObjectUE4Version.STATIC_MESH_REFACTOR)
        {
            Ar.Read<int>(); // VertexPositionVersionNumber
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DYNAMICTEXTUREINSTANCES && Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_CACHED_STATIC_MESH_STREAMING_FACTORS)
        {
            Ar.ReadArray<float>(); // CachedStreamingTextureFactors
        }

        if (!stripDataFlags.IsEditorDataStripped() && Ar.Ver >= EUnrealEngineObjectUE3Version.KEEP_STATIC_MESH_DEGENERATES && Ar.Ver < EUnrealEngineObjectUE4Version.STATIC_MESH_REFACTOR)
        {
            Ar.ReadBoolean(); // bRemoveDegenerates
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.INSTANCED_STATIC_MESH_PER_LOD_STATIC_LIGHTING && Ar.Game < GAME_UE4_0)
        {
            Ar.ReadBoolean(); // bPerLODStaticLightingForInstancing
            Ar.Read<int>(); // ConsolePreallocateInstanceCount
        }

        if (Ar.Ver > EUnrealEngineObjectUE4Version.STATIC_MESH_SOCKETS)
        {
            Sockets = Ar.ReadArray(() => new FPackageIndex(Ar));
        }

        if (!Ar.IsFilterEditorOnly || Ar.Game < GAME_UE4_0)
        {
            Ar.Position = validPos;
            return; // so it doesn't throw
        }

        // https://github.com/EpicGames/UnrealEngine/blob/ue5-main/Engine/Source/Runtime/Engine/Private/StaticMesh.cpp#L6701
        if (bCooked)
        {
            RenderData = Ar.Game switch
            {
                GAME_GameForPeace => new GFPStaticMeshRenderData(Ar, GetOrDefault<bool>("bIsStreamable")),
                GAME_WeHappyFew => new GFPStaticMeshRenderData(Ar, true),
                _ => RenderData = new FStaticMeshRenderData(Ar)
            };
        }

        if (Ar.Game == GAME_WutheringWaves && GetOrDefault<bool>("bUseKuroLODDistance") && Ar.ReadBoolean())
        {
            Ar.Position += 64; // 8 per-platform floats
        }

        if (Ar.Game is GAME_RocoKingdomWorld or GAME_SilverPalace) Ar.Position += 4;

        if (bCooked && Ar.Game is >= GAME_UE4_20 and < GAME_UE5_0 && Ar.Game != GAME_DreamStar) // DS removed this for some reason
        {
            var bHasOccluderData = Ar.ReadBoolean();
            if (bHasOccluderData)
            {
                switch (Ar.Game)
                {
                    case GAME_CrystalOfAtlan:
                    case GAME_FragPunk:
                    case GAME_RocoKingdomWorld:
                        if (Ar.Game is GAME_FragPunk && !Ar.ReadBoolean()) break;
                        Ar.SkipMultipleBulkArrayData(3);
                        break;
                    case GAME_Farlight84:
                    {
                        Ar.SkipMultipleBulkArrayData(2);
                        var count = Ar.Read<int>();
                        for (var i = 0; i < count; i++)
                            Ar.SkipMultipleBulkArrayData(2);
                        break;
                    }
                    case GAME_NeedForSpeedMobile:
                        Ar.SkipMultipleBulkArrayData(3);
                        Ar.Position += 4;
                        var count1 = Ar.Read<int>();
                        for (var i = 0; i < count1; i++)
                        {
                            Ar.Position += 4;
                            Ar.SkipMultipleFixedArrays(2, 4);
                        }
                        break;
                    case GAME_HonorofKingsWorld:
                        Ar.SkipBulkArrayData();
                        break;
                    case GAME_ArenaBreakoutMobile:
                    case GAME_ValorantSource:
                        Ar.SkipMultipleBulkArrayData(2);
                        break;
                    default:
                        Ar.SkipFixedArray(12); // Vertices
                        Ar.SkipFixedArray(2); // Indices
                        break;
                }
            }
        }

        switch (Ar.Game)
        {
            case GAME_FateTrigger or GAME_GhostsofTabor or GAME_Aion2:
                Ar.Position += 4;
                break;
            case GAME_TheFinals or GAME_ArcRaiders when Ar.ReadBoolean():
                Ar.SkipMultipleBulkArrayData(5);
                break;
            case GAME_ValorantSource when Ar.ReadBoolean():
                var count = Ar.Read<int>();
                for (var i = 0; i < count; i++)
                {
                    Ar.Position += 64;
                    Ar.SkipFixedArray(16);
                }
                Ar.SkipFixedArray(12);
                break;
            case GAME_PUBGLite when Ar.ReadBoolean():
                Ar.SkipMultipleBulkArrayData(2);
                break;
        }

        // (Ar.Ver >= EUnrealEngineObjectUE4Version.SPEEDTREE_STATICMESH), but we check UE version for Materials
        if (Ar.Game >= GAME_UE4_14)
        {
            var bHasSpeedTreeWind = Ar.ReadBoolean();
            if (bHasSpeedTreeWind)
            {
                Ar.Position = validPos;
                // return;
            }

            if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
            {
                // UE4.14+ - "Materials" are deprecated, added StaticMaterials
                StaticMaterials = bHasSpeedTreeWind ? GetOrDefault("StaticMaterials", Array.Empty<FStaticMaterial>()) : Ar.ReadArray(() => new FStaticMaterial(Ar));
            }
        }
        else if (TryGetValue(out FPackageIndex[] materials, "Materials"))
        {
            StaticMaterials = new FStaticMaterial[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                StaticMaterials[i] = new FStaticMaterial(materials[i]);
            }
        }

        Materials = new FPackageIndex?[StaticMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = StaticMaterials[i].MaterialInterface;
        }

        Ar.Position += Ar.Game switch
        {
            GAME_OutlastTrials => 1,
            GAME_Farlight84 or GAME_DuneAwakening => 4,
            GAME_DaysGone => Ar.Read<int>() * 4 + 4,
            _ => 0
        };
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("BodySetup");
        serializer.Serialize(writer, BodySetup);

        writer.WritePropertyName("NavCollision");
        serializer.Serialize(writer, NavCollision);

        writer.WritePropertyName("LightingGuid");
        serializer.Serialize(writer, LightingGuid);

        writer.WritePropertyName("RenderData");
        serializer.Serialize(writer, RenderData);
    }
}

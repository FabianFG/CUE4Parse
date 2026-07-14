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
    public FPackageIndex[] Sockets { get; private set; } // UStaticMeshSocket[]
    public FStaticMeshRenderData? RenderData { get; private set; }
    public FStaticMaterial[]? StaticMaterials { get; private set; }
    public ResolvedObject?[] Materials { get; private set; } // UMaterialInterface[]
    public int LODForCollision { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if(Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 12;
        base.Deserialize(Ar, validPos);
        Materials = [];
        LODForCollision = GetOrDefault(nameof(LODForCollision), 0);

        var stripDataFlags = new FStripDataFlags(Ar);
        bCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_MESH_REFACTOR && Ar.ReadBoolean();
        HasTangents = Ar.Ver >= EUnrealEngineObjectUE3Version.STATICMESH_VERTEXBUFFER_MERGE;

        if (Ar.Game == GAME_WutheringWaves && GetOrDefault<bool>("bUseStandaloneBodySetup"))
            BodySetup = GetOrDefault<FPackageIndex>("StandaloneBodySetup");
        else
            BodySetup = new FPackageIndex(Ar);

        if (Ar.Versions["StaticMesh.HasNavCollision"])
            NavCollision = new FPackageIndex(Ar);

        if (!stripDataFlags.IsEditorDataStripped())
        {
            if (Ar.Ver < EUnrealEngineObjectUE4Version.DEPRECATED_STATIC_MESH_THUMBNAIL_PROPERTIES_REMOVED)
            {
                 var dummyThumbnailAngle = new FRotator(Ar);
                 var dummyThumbnailDistance = Ar.Read<float>();
            }

            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.DeprecatedHighResSourceMesh)
            {
                var Deprecated_HighResSourceMeshName = Ar.ReadFString();
                var Deprecated_HighResSourceMeshCRC = Ar.Read<uint>();
            }
        }

        LightingGuid = Ar.Read<FGuid>(); // LocalLightingGuid
        Sockets = Ar.ReadArray(() => new FPackageIndex(Ar));

        if (!Ar.IsFilterEditorOnly)
        {
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

        if (Ar.Game is GAME_RocoKingdomWorld) Ar.Position += 4;

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

                Materials = new ResolvedObject[StaticMaterials.Length];
                for (var i = 0; i < Materials.Length; i++)
                {
                    Materials[i] = StaticMaterials[i].MaterialInterface;
                }
            }
        }
        else if (TryGetValue(out FPackageIndex[] materials, "Materials"))
        {
            Materials = new ResolvedObject[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                Materials[i] = materials[i].ResolvedObject!;
            }
        }

        Ar.Position += Ar.Game switch
        {
            GAME_OutlastTrials => 1,
            GAME_Farlight84 or GAME_DuneAwakening => 4,
            GAME_DaysGone => Ar.Read<int>() * 4 + 4,
            _ => 0
        };
    }

    public void OverrideMaterials(FPackageIndex[] materials)
    {
        for (var i = 0; i < materials.Length; i++)
        {
            if (i >= Materials.Length) break;
            if (materials[i].IsNull) continue;

            Materials[i] = materials[i].ResolvedObject;
        }
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

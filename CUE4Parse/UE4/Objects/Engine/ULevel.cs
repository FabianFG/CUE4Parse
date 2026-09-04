using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FStreamableTextureInstance(FAssetArchive Ar) : IUStruct
{
    public readonly FSphere BoundingSphere = new(Ar);
    public readonly float TexelFactor = Ar.Read<float>();
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FDynamicTextureInstance(FAssetArchive Ar) : IUStruct
{
    public readonly FSphere BoundingSphere = new(Ar);
    public readonly float TexelFactor = Ar.Read<float>();
    public readonly FPackageIndex Texture = new(Ar);
    public readonly bool bAttached = Ar.ReadBoolean();
    public readonly float OriginalRadius = Ar.Read<float>();
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FCachedPhysSMData(FAssetArchive Ar) : IUStruct
{
    public readonly FVector Scale3D = new(Ar);
    public readonly int CachedDataIndex = Ar.Read<int>();
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FCachedPerTriPhysSMData(FAssetArchive Ar) : IUStruct
{
    public readonly FVector Scale3D = new(Ar);
    public readonly int CachedDataIndex = Ar.Read<int>();
}

public class FStreamableTextureInfo(FAssetArchive Ar)
{
    public FPackageIndex Texture = new(Ar);
    public FStreamableTextureInstance[] TextureInstances = Ar.ReadArray(() => new FStreamableTextureInstance(Ar));
}

public class FStreamableSoundInstance(FAssetArchive Ar)
{
    public FSphere BoundingSphere = Ar.Read<FSphere>();
}

public class FStreamableSoundInfo(FAssetArchive Ar)
{
    public FPackageIndex SoundNodeWave = new(Ar);
    public FStreamableSoundInstance[] SoundInstances = Ar.ReadArray(() => new FStreamableSoundInstance(Ar));
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityCell : IUStruct
{
    public readonly FVector Min;
    public readonly ushort ChunkIndex;
    public readonly ushort DataOffset;

    public FPrecomputedVisibilityCell(FAssetArchive Ar)
    {
        Min = new FVector(Ar);
        ChunkIndex = Ar.Read<ushort>();
        DataOffset = Ar.Read<ushort>();
    }
}

[JsonConverter(typeof(FCompressedVisibilityChunkConverter))]
public readonly struct FCompressedVisibilityChunk : IUStruct
{
    public readonly bool bCompressed;
    public readonly int UncompressedSize;
    public readonly byte[] Data;

    public FCompressedVisibilityChunk(FAssetArchive Ar)
    {
        bCompressed = Ar.ReadBoolean();
        UncompressedSize = Ar.Read<int>();
        Data = [];
        Ar.SkipFixedArray(1);
    }
}

public readonly struct FPrecomputedVisibilityBucket : IUStruct
{
    public readonly int CellDataSize;
    public readonly FPrecomputedVisibilityCell[] Cells;
    public readonly FCompressedVisibilityChunk[] CellDataChunks;

    public FPrecomputedVisibilityBucket(FAssetArchive Ar)
    {
        CellDataSize = Ar.Read<int>();
        Cells = Ar.ReadArray(() => new FPrecomputedVisibilityCell(Ar));
        CellDataChunks = Ar.ReadArray(() => new FCompressedVisibilityChunk(Ar));
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityHandler : IUStruct
{
    public readonly FVector2D PrecomputedVisibilityCellBucketOriginXY;
    public readonly float PrecomputedVisibilityCellSizeXY;
    public readonly float PrecomputedVisibilityCellSizeZ;
    public readonly int PrecomputedVisibilityCellBucketSizeXY;
    public readonly int PrecomputedVisibilityNumCellBuckets;
    public readonly FPrecomputedVisibilityBucket[] PrecomputedVisibilityCellBuckets;

    public FPrecomputedVisibilityHandler(FAssetArchive Ar)
    {
        PrecomputedVisibilityCellBucketOriginXY = new FVector2D(Ar);
        PrecomputedVisibilityCellSizeXY = Ar.Read<float>();
        PrecomputedVisibilityCellSizeZ = Ar.Read<float>();
        PrecomputedVisibilityCellBucketSizeXY = Ar.Read<int>();
        PrecomputedVisibilityNumCellBuckets = Ar.Read<int>();
        PrecomputedVisibilityCellBuckets = Ar.ReadArray(() => new FPrecomputedVisibilityBucket(Ar));
        if (Ar.Game is GAME_IntotheRadius2)
        {
            _ = Ar.ReadArray(() => new FCompressedVisibilityChunk(Ar));
            Ar.Position += 57;
        }
        else if (Ar.Game is GAME_TheDivisionResurgence)
        {
            Ar.SkipFixedArray(8);
            _ = Ar.ReadArray(() => new FPrecomputedVisibilityBucket(Ar));
        }
    }
}

public readonly struct FPrecomputedVolumeDistanceField : IUStruct
{
    public readonly float VolumeMaxDistance;
    public readonly FBox VolumeBox;
    public readonly int VolumeSizeX;
    public readonly int VolumeSizeY;
    public readonly int VolumeSizeZ;
    public readonly FColor[] Data;

    public FPrecomputedVolumeDistanceField(FAssetArchive Ar)
    {
        VolumeMaxDistance = Ar.Read<float>();
        VolumeBox = new FBox(Ar);
        VolumeSizeX = Ar.Read<int>();
        VolumeSizeY = Ar.Read<int>();
        VolumeSizeZ = Ar.Read<int>();
        if (Ar.Game is GAME_LordOfMysteries) return;
        Data = Ar.ReadArray<FColor>();
    }
}

public class ULevel : Assets.Exports.UObject, IAssetUserData
{
    public FPackageIndex WorldSettings;
    public FPackageIndex WorldDataLayers;
    public FSoftObjectPath WorldPartitionRuntimeCell;
    public FPackageIndex[]? AssetUserData { get; private set; }

    public FPackageIndex?[] Actors;
    public FURL URL;
    public FPackageIndex Model;
    public FPackageIndex[] ModelComponents;
    public FPackageIndex LevelScriptActor;
    public FPackageIndex? NavListStart;
    public FPackageIndex? NavListEnd;
    public FPrecomputedVisibilityHandler? PrecomputedVisibilityHandler;
    public FPrecomputedVolumeDistanceField? PrecomputedVolumeDistanceField;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        WorldSettings = GetOrDefault(nameof(WorldSettings), new FPackageIndex());
        WorldDataLayers = GetOrDefault(nameof(WorldDataLayers), new FPackageIndex());
        WorldPartitionRuntimeCell = GetOrDefault<FSoftObjectPath>(nameof(WorldPartitionRuntimeCell));
        AssetUserData = GetOrDefault<FPackageIndex[]?>(nameof(AssetUserData));

        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 16;
        if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject) || Ar.Position >= validPos) return;
        if (FReleaseObjectVersion.Get(Ar) < FReleaseObjectVersion.Type.LevelTransArrayConvertedToTArray) Ar.Position += 4;
        Actors = Ar.ReadArray(() => new FPackageIndex(Ar));
        URL = new FURL(Ar);
        Model = new FPackageIndex(Ar);
        ModelComponents = Ar.ReadArray(() => new FPackageIndex(Ar));

        if (Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_USEQUENCE)
        {
            Ar.ReadArray(() => new FPackageIndex(Ar)); // GameSequences
        }

        if (Ar.Game >= GAME_UE4_0)
        {
            LevelScriptActor = new FPackageIndex(Ar);
        }
        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.RemovedTextureStreamingLevelData)
        {
            if (Ar.Ver < EUnrealEngineObjectUE3Version.SPLIT_SOUND_FROM_TEXTURE_STREAMING)
            {
                Ar.ReadArray(() => new FStreamableTextureInfo(Ar)); // ResourceInfos (FStreamableResourceInstance but same struct as FStreamableTextureInfo)
            }
            else if (Ar.Ver < EUnrealEngineObjectUE3Version.RENDERING_REFACTOR)
            {
                Ar.ReadArray(() => new FStreamableTextureInfo(Ar)); // TextureInfos
                Ar.ReadArray(() => new FStreamableSoundInfo(Ar)); // SoundInfos
            }
            else
            {
                Ar.ReadMap(() => new FPackageIndex(Ar), () => Ar.ReadArray(() => new FStreamableTextureInstance(Ar))); // TextureToInstancesMap
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.DYNAMICTEXTUREINSTANCES && Ar.Game is not GAME_Dishonored)
            {
                Ar.ReadMap(() => new FPackageIndex(Ar), () => Ar.ReadArray(() => new FDynamicTextureInstance(Ar))); // DynamicTextureInstances
            }

            var bIsCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.REBUILD_TEXTURE_STREAMING_DATA_ON_LOAD && Ar.ReadBoolean();

            if (Ar.Game >= GAME_UE4_0 && Ar.Ver < EUnrealEngineObjectUE4Version.REBUILD_TEXTURE_STREAMING_DATA_ON_LOAD)
            {
                Ar.ReadBoolean(); // bTextureStreamingBuiltDummy
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.APEX_DESTRUCTION)
            {
                Ar.ReadArray<byte>();
            }

            if (Ar.Game >= GAME_UE4_0 && Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_LEVELBODYSETUP)
            {
                new FPackageIndex(Ar); // DummySetup
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRECOOK_PHYS_BSP_TERRAIN && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadBulkArray<byte>(); // CachedPhysBSPData
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRECOOK_PHYS_STATICMESH_CACHE && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadMap(() => new FPackageIndex(Ar), () => new FCachedPhysSMData(Ar)); // CachedPhysSMDataMap
                Ar.ReadArray(() => Ar.ReadArray(() => Ar.ReadBulkArray<byte>())); // CachedPhysSMDataStore
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRECOOK_PERTRI_PHYS_STATICMESH && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadMap(() => new FPackageIndex(Ar), () => new FCachedPhysSMData(Ar)); // CachedPhysSMDataMap
                Ar.ReadArray(() => Ar.ReadBulkArray<byte>()); // CachedPhysPerTriSMDataStore
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.SAVE_PRECOOK_PHYS_VERSION && Ar.Game < GAME_UE4_0)
            {
                Ar.Read<int>(); // CachedPhysBSPDataVersion
                Ar.Read<int>(); // CachedPhysSMDataVersion
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.LEVEL_FORCE_STREAM_TEXTURES)
            {
                Ar.ReadMap(() => new FPackageIndex(Ar), () => Ar.ReadBoolean()); // ForceStreamTextures
            }

            if (Ar.Ver > EUnrealEngineObjectUE3Version.CONVEX_BSP && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadArray(() => Ar.ReadArray(() => Ar.ReadBulkArray<byte>())); // CachedPhysConvexBSPData
                Ar.Read<int>(); // CachedPhysConvexBSPVersion
            }
        }
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.PERLEVEL_NAVLIST)
        {
            NavListStart = new FPackageIndex(Ar);
            NavListEnd = new FPackageIndex(Ar);

            if (Ar.Game < GAME_UE4_0)
            {
                new FPackageIndex(Ar); // CoverListStart
                new FPackageIndex(Ar); // CoverListEnd
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.PYLONLIST_IN_ULEVEL && Ar.Ver < EUnrealEngineObjectUE4Version.REMOVED_OLD_NAVMESH)
            {
                new FPackageIndex(Ar); // LegacyPylonListStart
                new FPackageIndex(Ar); // LegacyPylonListEnd
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.COVERGUIDREFS_IN_ULEVEL && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadMap(Ar.Read<FGuid>, Ar.Read<int>); // CrossLevelCoverGuidRefs
                Ar.ReadArray(() => new FPackageIndex(Ar)); // CoverLinkRefs
                Ar.ReadMap(Ar.Read<int>, Ar.Read<byte>); // CoverIndexPairs
            }

            if (Ar.Game < GAME_UE4_0)
            {
                Ar.ReadArray(() => new FPackageIndex(Ar)); // CrossLevelActors
            }
        }
        if (Ar.Game == GAME_MetroAwakening && GetOrDefault<bool>("bIsLightingScenario")) return;
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.GI_CHARACTER_LIGHTING && FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
        {
            _ = new FPrecomputedLightVolumeData(Ar, false);
        }
        if (Ar.Game == GAME_OutlastTrials)
        {
            PrecomputedVolumeDistanceField = new FPrecomputedVolumeDistanceField(Ar);
            return;
        }
        if (Ar.Game is GAME_LordOfMysteries) Ar.Position += 8;
        if (Ar.Game is GAME_ValorantSource) Ar.Position += 28;
        if (Ar.Game is GAME_TamasShadowveil) return;
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.NONUNIFORM_PRECOMPUTED_VISIBILITY)
        {
            PrecomputedVisibilityHandler = new FPrecomputedVisibilityHandler(Ar);
        }
        else if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRECOMPUTED_VISIBILITY)
        {
            new FBox(Ar); // LegacyPrecomputedVisibilityVolume
            Ar.Read<float>(); // LegacyPrecomputedVisibilityCellSize
            Ar.ReadArray(() => Ar.ReadArray<byte>()); // LegacyPrecomputedVisibilityData
        }
        if (Ar.Game is GAME_AssaultFireFuture && Ar.Read<int>() != 0) return;
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.IMAGE_REFLECTION_SHADOWING)
        {
            PrecomputedVolumeDistanceField = new FPrecomputedVolumeDistanceField(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO && Ar.Ver < EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO_UPDATED)
        {
            new FWorldTileInfo(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Actors");
        serializer.Serialize(writer, Actors);

        writer.WritePropertyName("URL");
        serializer.Serialize(writer, URL);

        writer.WritePropertyName("Model");
        serializer.Serialize(writer, Model);

        writer.WritePropertyName("ModelComponents");
        serializer.Serialize(writer, ModelComponents);

        writer.WritePropertyName("LevelScriptActor");
        serializer.Serialize(writer, LevelScriptActor);

        writer.WritePropertyName("NavListStart");
        serializer.Serialize(writer, NavListStart);

        writer.WritePropertyName("NavListEnd");
        serializer.Serialize(writer, NavListEnd);

        if (PrecomputedVisibilityHandler == null) return;

        writer.WritePropertyName("PrecomputedVisibilityHandler");
        serializer.Serialize(writer, PrecomputedVisibilityHandler);

        writer.WritePropertyName("PrecomputedVolumeDistanceField");
        serializer.Serialize(writer, PrecomputedVolumeDistanceField);
    }
}

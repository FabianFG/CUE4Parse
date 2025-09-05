using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public class UWorldComposition : UObject
{
    // Path to current world composition (long PackageName)
    public string? WorldRoot;
    // List of all tiles participating in the world composition
    public FWorldCompositionTile[]? Tiles;
    // Streaming level objects for each tile
    public FPackageIndex[]? TilesStreaming;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position >= validPos) return;
        WorldRoot = Ar.ReadFString();
        Tiles = Ar.ReadArray(() => new FWorldCompositionTile(Ar));
        TilesStreaming = Ar.ReadArray(() => new FPackageIndex(Ar));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(WorldRoot));
        writer.WriteValue(WorldRoot);
        writer.WritePropertyName(nameof(Tiles));
        serializer.Serialize(writer, Tiles);
        writer.WritePropertyName(nameof(TilesStreaming));
        serializer.Serialize(writer, TilesStreaming);
    }
}

public class FWorldCompositionTile
{
    // Long package name
    public FName PackageName;
    // Found LOD levels since last rescan
    public FName[] LODPackageNames;
    // Tile information
    public FWorldTileInfo Info;

    public FWorldCompositionTile(FAssetArchive Ar)
    {
        PackageName = Ar.ReadFName();
        Info = new FWorldTileInfo(Ar);
        LODPackageNames = Ar.ReadArray(Ar.ReadFName);
    }
}

public class FWorldTileInfo
{
    /** Tile position in the world relative to parent */
    public FIntVector Position;
    /** Absolute tile position in the world. Calculated in runtime */
    [JsonIgnore] public FIntVector AbsolutePosition;
    /** Tile bounding box  */
    public FBox Bounds;
    /** Tile assigned layer  */
    public FWorldTileLayer Layer;
    /** Whether to hide sub-level tile in tile view*/
    public bool bHideInTileView;
    /** Parent tile package name */
    public string ParentTilePackageName;
    /** LOD information */
    public FWorldTileLODInfo[] LODList;
    /** Sorting order */
    public int ZOrder;

    public FWorldTileInfo(FAssetArchive Ar)
    {
        if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.WorldCompositionTile3DOffset)
        {
            Position = new FIntVector(Ar.Read<int>(), Ar.Read<int>(), 0);
        }
        else
        {
            Position = Ar.Read<FIntVector>();
        }
        Bounds = new FBox(Ar);
        Layer = new FWorldTileLayer(Ar);
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO_UPDATED)
        {
            bHideInTileView = Ar.ReadBoolean();
            ParentTilePackageName = Ar.ReadFString();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO_LOD_LIST)
        {
            LODList = Ar.ReadArray<FWorldTileLODInfo>();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO_ZORDER)
        {
            ZOrder = Ar.Read<int>();
        }

        if (Ar.Ver < EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
        {
            Ar.Position += 8; // idk
        }
    }
}

public class FWorldTileLayer
{
    /** Human readable name for this layer */
    public string Name;
    [JsonIgnore] public int Reserved0;
    [JsonIgnore]public FIntPoint Reserved1;
    /** Distance starting from where tiles belonging to this layer will be streamed in */
    public int StreamingDistance;
    public bool DistanceStreamingEnabled;

    public FWorldTileLayer(FAssetArchive Ar)
    {
        Name = Ar.ReadFString();
        Reserved0 = Ar.Read<int>();
        Reserved1 = Ar.Read<FIntPoint>();
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LEVEL_INFO_UPDATED)
        {
            StreamingDistance = Ar.Read<int>();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.WORLD_LAYER_ENABLE_DISTANCE_STREAMING)
        {
            DistanceStreamingEnabled = Ar.ReadBoolean();
        }
    }
}

public struct FWorldTileLODInfo
{
    /**  Relative to LOD0 streaming distance, absolute distance = LOD0 + StreamingDistanceDelta */
    public int RelativeStreamingDistance;
    /**  Reserved for additional options */
    [JsonIgnore] public float Reserved0;
    [JsonIgnore] public float Reserved1;
    [JsonIgnore] public int Reserved2;
    [JsonIgnore] public int Reserved3;
}

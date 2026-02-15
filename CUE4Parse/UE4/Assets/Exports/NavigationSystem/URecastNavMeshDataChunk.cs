using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.NavigationSystem.NavMesh;

public class URecastNavMeshDataChunk : Assets.Exports.UObject
{
    public ENavMeshVersion NavMeshVersion;
    public FRecastTileData[] Tiles = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game is EGame.GAME_OuterWorlds2) return;

        NavMeshVersion = Ar.Read<ENavMeshVersion>();
        var recastNavMeshSizePos = Ar.Position;
        var recastNavMeshSizeBytes = Ar.Read<long>();

        if (NavMeshVersion < ENavMeshVersion.NAVMESHVER_MIN_COMPATIBLE)
        {
            Log.Error("NavMeshVersion is too old and not supported: '{0}'", NavMeshVersion);
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
        }
        else if (NavMeshVersion > ENavMeshVersion.Latest)
        {
            Log.Error("NavMeshVersion is too new and not supported: '{0}'", NavMeshVersion);
            Ar.Position = recastNavMeshSizePos + recastNavMeshSizeBytes;
        }
        else if (recastNavMeshSizeBytes > 4)
        {
            Tiles = Ar.ReadArray(() => new FRecastTileData(Ar, NavMeshVersion));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(NavMeshVersion));
        writer.WriteValue(NavMeshVersion);
        writer.WritePropertyName(nameof(Tiles));
        serializer.Serialize(writer, Tiles);
    }
}

public class FRecastTileData
{
    public int TileDataSize;
    public FDetourTileSizeInfo? SizeInfo;
    [JsonIgnore] public DetourMeshTile? Tile;
    public FCompressedTileCacheData? CompressedTileCacheData;

    public bool IsValid => TileDataSize > 0 && Tile != null;

    public FRecastTileData(FArchive Ar, ENavMeshVersion NavMeshVersion)
    {
        TileDataSize = Ar.Read<int>();
        if (TileDataSize <= 0) return;

        SizeInfo = new FDetourTileSizeInfo(Ar);
        Tile = new DetourMeshTile(Ar, SizeInfo, NavMeshVersion);
        CompressedTileCacheData = FPImplRecastNavMesh.SerializeCompressedTileCacheData(Ar);
    }
}

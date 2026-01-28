using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;
using CUE4Parse.UE4.Assets.Exports.NavigationSystem;

namespace CUE4Parse.UE4.Objects.NavigationSystem.NavMesh;

public class URecastNavMeshDataChunk : Assets.Exports.UObject
{
    public ENavMeshVersion NavMeshVersion;
    public long RecastNavMeshSizeBytes;
    public FRecastTileData[] Tiles;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game is EGame.GAME_OuterWorlds2) return;

        NavMeshVersion = Ar.Read<ENavMeshVersion>();
        RecastNavMeshSizeBytes = Ar.Read<long>();
        if ((int)NavMeshVersion < 17)
        {
            Ar.Position += RecastNavMeshSizeBytes;
            Log.Warning("URecastNavMeshDataChunk: NavMeshVersion is too old and not supported: '{0}'", NavMeshVersion);
            return;
        }

        var TileNum = Ar.Read<int>();
        Tiles = new FRecastTileData[TileNum];
        for (var i = 0; i < TileNum; i++)
        {
            Tiles[i] = new FRecastTileData(Ar, NavMeshVersion);
            FPImplRecastNavMesh.SerializeCompressedTileCacheData(Ar);
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

    public class FRecastTileData
    {
        public int TileDataSize;
        public FDetourTileSizeInfo? SizeInfo;
        [JsonIgnore] public DetourMeshTile? Tile;

        public FRecastTileData(FAssetArchive Ar, ENavMeshVersion NavMeshVersion)
        {
            TileDataSize = Ar.Read<int>();
            if (TileDataSize <= 0) return;
            SizeInfo = new FDetourTileSizeInfo(Ar);
            Tile = new DetourMeshTile(Ar, SizeInfo, NavMeshVersion);
        }
    }
}

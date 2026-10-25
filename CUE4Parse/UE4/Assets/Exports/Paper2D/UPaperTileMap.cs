using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

public enum ETileMapProjectionMode : byte
{
    Orthogonal,
    IsometricDiamond,
    IsometricStaggered,
    HexagonalStaggered
}

public class UPaperTileMap : UObject
{
    public int MapWidth = 4;
    public int MapHeight = 4;
    public int TileWidth = 32;
    public int TileHeight = 32;
    public float PixelsPerUnrealUnit = 1f;
    public ETileMapProjectionMode ProjectionMode;
    public FPackageIndex[] TileLayers = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        MapWidth = GetOrDefault(nameof(MapWidth), 4);
        MapHeight = GetOrDefault(nameof(MapHeight), 4);
        TileWidth = GetOrDefault(nameof(TileWidth), 32);
        TileHeight = GetOrDefault(nameof(TileHeight), 32);
        PixelsPerUnrealUnit = GetOrDefault(nameof(PixelsPerUnrealUnit), 1f);
        ProjectionMode = GetOrDefault<ETileMapProjectionMode>(nameof(ProjectionMode));
        TileLayers = GetOrDefault<FPackageIndex[]>(nameof(TileLayers), []);
    }
}

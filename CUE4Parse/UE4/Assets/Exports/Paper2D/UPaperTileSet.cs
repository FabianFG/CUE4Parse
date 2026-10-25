using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

public class UPaperTileSet : UObject
{
    public FIntPoint TileSize = new(32, 32);
    public FPackageIndex? TileSheet;
    public FPackageIndex[] AdditionalSourceTextures = [];
    public FIntMargin BorderMargin;
    public FIntPoint PerTileSpacing;
    public FIntPoint DrawingOffset;
    public int WidthInTiles;
    public int HeightInTiles;
    public int AllocatedWidth;
    public int AllocatedHeight;
    public FPaperTileMetadata[] PerTileData = [];
    public FPaperTileSetTerrain[] Terrains = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        TileSize = GetOrDefault(nameof(TileSize), new FIntPoint(GetOrDefault("TileWidth_DEPRECATED", 32), GetOrDefault("TileHeight_DEPRECATED", 32)));
        TileSheet = GetOrDefault<FPackageIndex?>(nameof(TileSheet));
        AdditionalSourceTextures = GetOrDefault<FPackageIndex[]>(nameof(AdditionalSourceTextures), []);
        BorderMargin = GetOrDefault(nameof(BorderMargin), new FIntMargin(GetOrDefault("Margin_DEPRECATED", 0)));
        var spacing = GetOrDefault("Spacing_DEPRECATED", 0);
        PerTileSpacing = GetOrDefault(nameof(PerTileSpacing), new FIntPoint(spacing, spacing));
        DrawingOffset = GetOrDefault<FIntPoint>(nameof(DrawingOffset));
        WidthInTiles = GetOrDefault<int>(nameof(WidthInTiles));
        HeightInTiles = GetOrDefault<int>(nameof(HeightInTiles));
        AllocatedWidth = GetOrDefault<int>(nameof(AllocatedWidth));
        AllocatedHeight = GetOrDefault<int>(nameof(AllocatedHeight));
        PerTileData = GetOrDefault<FPaperTileMetadata[]>(nameof(PerTileData), []);
        Terrains = GetOrDefault<FPaperTileSetTerrain[]>(nameof(Terrains), []);
    }

    public FPaperTileMetadata? GetTileMetadata(int tileIndex) => (uint) tileIndex < (uint) PerTileData.Length ? PerTileData[tileIndex] : null;
    public FName GetTileUserData(int tileIndex) => GetTileMetadata(tileIndex)?.UserDataName ?? default;
    public FIntPoint GetTileUVFromTileXY(FIntPoint tileXY) => new(tileXY.X * (TileSize.X + PerTileSpacing.X) + BorderMargin.Left, tileXY.Y * (TileSize.Y + PerTileSpacing.Y) + BorderMargin.Top);
}

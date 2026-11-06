using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

[Flags]
public enum EPaperTileFlags : uint
{
    FlipHorizontal = 1U << 31,
    FlipVertical = 1U << 30,
    FlipDiagonal = 1U << 29,
    TileIndexMask = ~(7U << 29)
}

[StructFallback]
public readonly struct FPaperTileInfo
{
    public readonly FPackageIndex? TileSet;
    public readonly int PackedTileIndex;

    public FPaperTileInfo()
    {
        TileSet = null;
        PackedTileIndex = -1;
    }

    public FPaperTileInfo(FStructFallback fallback)
    {
        TileSet = fallback.GetOrDefault<FPackageIndex?>(nameof(TileSet));
        PackedTileIndex = fallback.GetOrDefault(nameof(PackedTileIndex), -1);
    }

    public bool IsValid() => TileSet is { IsNull: false } && PackedTileIndex != -1;
    public int GetTileIndex() => PackedTileIndex & (int) EPaperTileFlags.TileIndexMask;
    public int GetFlagsAsIndex() => (int) ((uint) PackedTileIndex >> 29);
    public bool HasFlag(EPaperTileFlags flag) => ((uint) PackedTileIndex & (uint) flag) != 0;
}

using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

[StructFallback]
public readonly struct FPaperTileSetTerrain(FStructFallback fallback)
{
    public readonly string TerrainName = fallback.GetOrDefault(nameof(TerrainName), string.Empty);
    public readonly int CenterTileIndex = fallback.GetOrDefault<int>(nameof(CenterTileIndex));
}

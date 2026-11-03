using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

internal static class FProceduralEscapeLayout
{
    internal static readonly int[] _outwardFlags = [0, 6, 3, 5];

    public static IReadOnlyList<FProceduralTilePlacement> Build(FProceduralTileCell[] cells, int width, int height, IReadOnlyList<FProceduralTile> tiles, int seed, int count, CancellationToken ct,
        IReadOnlyList<FProceduralTilePlacement>? existingEscapes = null)
    {
        if (count <= 0)
            return [];

        var escapeTiles = tiles.Where(tile => tile.IsEscapeTile && tile.Width == 1 && tile.Height == 1 && double.IsFinite(tile.Weight) && tile.Weight > 0)
            .OrderBy(tile => tile.AssetPath, StringComparer.Ordinal).ToArray();
        var candidates = new List<FProceduralTilePlacement>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                ct.ThrowIfCancellationRequested();
                var cell = cells[y * width + x];
                if (cell.Type is not ETileType.Blocker || !TryGetFlags(cells, width, height, x, y, out var flags))
                    continue;

                foreach (var tile in escapeTiles)
                {
                    if ((cell.Number == 0 || tile.Number == 0 || cell.Number == tile.Number) && cell.Path == tile.PathType && tile.MatchesDensity(cell.Density))
                        // EscapeTile::CanSpawn faces the exit outwards, independently of the path layer's rotation (otherwise Hawkins escape tile is incorrectly rotated)
                        candidates.Add(new FProceduralTilePlacement(tile, x, y, flags));
                }
            }
        }

        existingEscapes ??= [];
        candidates.RemoveAll(candidate => existingEscapes.Any(existing =>
            candidate.X >= existing.X && candidate.X < existing.X + existing.Width && candidate.Y >= existing.Y && candidate.Y < existing.Y + existing.Height ||
            Math.Max(Math.Abs(candidate.X - existing.X), Math.Abs(candidate.Y - existing.Y)) <= Math.Max(existing.Tile.MinimumSpacing, candidate.Tile.MinimumSpacing)) ||
            candidate.Tile.MaximumCount > 0 && existingEscapes.Count(existing => existing.Tile == candidate.Tile) >= candidate.Tile.MaximumCount);

        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.TileSelection);
        var result = new List<FProceduralTilePlacement>();
        while (result.Count < count && candidates.Count > 0)
        {
            var weight = random.NextFloat() * candidates.Sum(candidate => candidate.Tile.Weight);
            var selected = candidates[^1];
            foreach (var candidate in candidates)
            {
                weight -= candidate.Tile.Weight;
                if (weight >= 0)
                    continue;
                selected = candidate;
                break;
            }

            result.Add(selected);
            candidates.RemoveAll(candidate =>
                Math.Max(Math.Abs(candidate.X - selected.X), Math.Abs(candidate.Y - selected.Y)) <= Math.Max(selected.Tile.MinimumSpacing, candidate.Tile.MinimumSpacing) ||
                candidate.Tile.MaximumCount > 0 && result.Count(placement => placement.Tile == candidate.Tile) + existingEscapes.Count(placement => placement.Tile == candidate.Tile) >= candidate.Tile.MaximumCount);
        }

        return result;
    }

    internal static bool CanSpawn(FProceduralTileCell[] cells, int width, int height, FProceduralTilePlacement placement)
    {
        if (placement.X + placement.Width > width || placement.Y + placement.Height > height)
            return false;

        for (var y = placement.Y; y < placement.Y + placement.Height; y++)
        {
            for (var x = placement.X; x < placement.X + placement.Width; x++)
            {
                if (TryGetFlags(cells, width, height, x, y, out var flags) && flags == placement.Flags)
                    return true;
            }
        }

        return false;
    }

    internal static bool TryGetFlags(FProceduralTileCell[] cells, int width, int height, int x, int y, out int flags)
    {
        var left = GetType(x - 1, y);
        var right = GetType(x + 1, y);
        var up = GetType(x, y - 1);
        var down = GetType(x, y + 1);

        flags = 0;
        if (left is ETileType.Blocker && right is ETileType.Blocker)
        {
            if (down > ETileType.Blocker && up is ETileType.None)
                return true;

            if (up > ETileType.Blocker && down is ETileType.None)
            {
                flags = 6;
                return true;
            }
        }
        else if (up is ETileType.Blocker && down is ETileType.Blocker)
        {
            if (right > ETileType.Blocker && left is ETileType.None)
            {
                flags = 3;
                return true;
            }

            if (left > ETileType.Blocker && right is ETileType.None)
            {
                flags = 5;
                return true;
            }
        }

        return false;

        ETileType GetType(int cellX, int cellY) => cellX >= 0 && cellX < width && cellY >= 0 && cellY < height ? cells[cellY * width + cellX].Type : ETileType.None;
    }
}

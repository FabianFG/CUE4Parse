using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

internal sealed class FProceduralTileLayout(FProceduralTileCell[] cells, int mapWidth, int mapHeight, IReadOnlyList<FProceduralTile> tiles, int seed, int escapeDoorCount)
{
    private FProceduralRandomStream _random = new(seed, EProceduralRandomStream.TileSelection);
    private readonly bool[] _occupiedCells = new bool[cells.Length];
    private readonly List<FProceduralTilePlacement> _placements = [];
    private readonly Dictionary<FProceduralTile, List<FProceduralTilePlacement>> _placementsByTile = [];

    public static FTransform EnvironmentTransform(FVector2D tileSize, int mapWidth, int mapHeight) => new(new FVector((1 - mapWidth) * tileSize.X / 2f, (1 - mapHeight) * tileSize.Y / 2f, 0));

    public static List<FProceduralTilePlacement> Build(FProceduralTileCell[] cells, int mapWidth, int mapHeight, IReadOnlyList<FProceduralTile> tiles, int seed, int escapeDoorCount = 0, CancellationToken ct = default)
        => new FProceduralTileLayout(cells, mapWidth, mapHeight, tiles, seed, escapeDoorCount).Build(ct);

    private List<FProceduralTilePlacement> Build(CancellationToken ct)
    {
        for (var y = 0; y < mapHeight; y++)
        {
            for (var x = 0; x < mapWidth; x++)
            {
                ct.ThrowIfCancellationRequested();
                var cellIndex = y * mapWidth + x;
                var cell = cells[cellIndex];
                if (_occupiedCells[cellIndex] || cell.Type is ETileType.None)
                    continue;

                var placement = SelectPlacement(cell, x, y) ?? throw new InvalidDataException($"No matching DBD tile at ({x}, {y}): type={cell.Type}, density={cell.Density}, path={cell.Path}, number={cell.Number}.");
                AddPlacement(placement);
            }
        }

        // Escape generation replaces border tiles after the regular layout is selected
        var missingEscapes = Math.Max(0, escapeDoorCount - _placements.Count(placement => placement.Tile.IsEscapeTile));
        for (var index = 0; index < _placements.Count && missingEscapes > 0; index++)
        {
            var current = _placements[index];
            if (current.Tile.IsEscapeTile)
                continue;

            var replacements = tiles.Where(tile => tile.IsEscapeTile && tile.Width * tile.Height > 1 &&
                    tile.Type == current.Tile.Type && tile.PathType == current.Tile.PathType && tile.Number == current.Tile.Number &&
                    tile.MatchesDensity(cells[current.Y * mapWidth + current.X].Density) && double.IsFinite(tile.Weight) && tile.Weight > 0 &&
                    (tile.MaximumCount <= 0 || _placements.Count(placement => placement.Tile == tile) < tile.MaximumCount))
                .Select(tile => new FProceduralTilePlacement(tile, current.X, current.Y, current.Flags))
                .Where(placement => placement.Width == current.Width && placement.Height == current.Height && FProceduralEscapeLayout.CanSpawn(cells, mapWidth, mapHeight, placement))
                .OrderBy(placement => placement.Tile.AssetPath, StringComparer.Ordinal).ToArray();
            if (replacements.Length == 0)
                continue;

            _placements[index] = SelectWeighted(replacements);
            missingEscapes--;
        }

        var existingEscapes = _placements.Where(placement => placement.Tile.IsEscapeTile).ToArray();
        var remainingEscapes = Math.Max(0, escapeDoorCount - existingEscapes.Length);
        foreach (var escape in FProceduralEscapeLayout.Build(cells, mapWidth, mapHeight, tiles, seed, remainingEscapes, ct, existingEscapes))
        {
            var index = _placements.FindIndex(placement => placement.X == escape.X && placement.Y == escape.Y && placement.Width == 1 && placement.Height == 1);
            if (index >= 0)
                _placements[index] = escape;
        }

        return _placements;
    }

    private FProceduralTilePlacement? SelectPlacement(FProceduralTileCell cell, int x, int y)
    {
        var bestGroup = GetCandidates(cell, x, y)
            .GroupBy(placement => (
                NegativeArea: -placement.Width * placement.Height,
                IsEscapeTile: placement.Tile.IsEscapeTile,
                IsReplacement: placement.Tile.bReplacement,
                ReachedCountLimit: HasReachedMaximumCount(placement.Tile),
                ViolatesSpacing: ViolatesMinimumSpacing(placement)))
            .MinBy(group => group.Key);

        if (bestGroup == null)
            return null;

        return SelectWeighted([.. bestGroup.OrderBy(placement => placement.Tile.AssetPath, StringComparer.Ordinal)]);
    }

    private FProceduralTilePlacement SelectWeighted(FProceduralTilePlacement[] candidates)
    {
        var remainingWeight = _random.NextFloat() * candidates.Sum(placement => placement.Tile.Weight);
        foreach (var candidate in candidates)
        {
            if (remainingWeight < candidate.Tile.Weight)
                return candidate;

            remainingWeight -= candidate.Tile.Weight;
        }

        return candidates[^1];
    }

    private IEnumerable<FProceduralTilePlacement> GetCandidates(FProceduralTileCell cell, int x, int y)
    {
        foreach (var tile in tiles)
        {
            if (tile.IsEscapeTile && (tile.PathType is EPathType.None || tile.Number == 0))
                continue;
            if (!double.IsFinite(tile.Weight) || tile.Weight <= 0.0)
                continue;
            if (cell.Type is not ETileType.Any && tile.Type != cell.Type)
                continue;
            if (!tile.MatchesDensity(cell.Density))
                continue;
            if (cell.Number != 0 && tile.Number != cell.Number)
                continue;
            if (tile.PathType != cell.Path)
                continue;

            if (tile.IsEscapeTile)
            {
                foreach (var flags in FProceduralEscapeLayout._outwardFlags)
                {
                    var escape = new FProceduralTilePlacement(tile, x, y, flags);
                    if (FProceduralEscapeLayout.CanSpawn(cells, mapWidth, mapHeight, escape) && CanPlace(escape, cell))
                        yield return escape;
                }

                continue;
            }

            var placement = new FProceduralTilePlacement(tile, x, y, cell.Flags);
            if (CanPlace(placement, cell))
                yield return placement;

            // An unoriented rectangular tile can also fit across the other grid axis
            if (cell.Path is not EPathType.None || cell.Flags != 0 || tile.Width == tile.Height)
                continue;

            var rotatedPlacement = new FProceduralTilePlacement(tile, x, y, FProceduralTilePlacement.QuarterTurnFlags);
            if (CanPlace(rotatedPlacement, cell))
                yield return rotatedPlacement;
        }
    }

    private bool CanPlace(FProceduralTilePlacement placement, FProceduralTileCell sourceCell)
    {
        var endX = placement.X + placement.Width;
        var endY = placement.Y + placement.Height;
        if (endX > mapWidth || endY > mapHeight)
            return false;

        for (var y = placement.Y; y < endY; y++)
        {
            for (var x = placement.X; x < endX; x++)
            {
                var cellIndex = y * mapWidth + x;
                if (_occupiedCells[cellIndex])
                    return false;

                var cell = cells[cellIndex];
                if (cell.Type != sourceCell.Type || !placement.Tile.MatchesDensity(cell.Density) || cell.Path != sourceCell.Path || cell.Number != sourceCell.Number || cell.Flags != sourceCell.Flags)
                    return false;
            }
        }

        return true;
    }

    private bool HasReachedMaximumCount(FProceduralTile tile) => tile.MaximumCount > 0 && _placementsByTile.TryGetValue(tile, out var placements) && placements.Count >= tile.MaximumCount;

    private bool ViolatesMinimumSpacing(FProceduralTilePlacement placement) =>
        _placementsByTile.TryGetValue(placement.Tile, out var previousPlacements) && previousPlacements.Any(previous =>
            Math.Max(Math.Abs(previous.X - placement.X), Math.Abs(previous.Y - placement.Y)) <= placement.Tile.MinimumSpacing);

    private void AddPlacement(FProceduralTilePlacement placement)
    {
        _placements.Add(placement);
        if (!_placementsByTile.TryGetValue(placement.Tile, out var tilePlacements))
        {
            tilePlacements = [];
            _placementsByTile.Add(placement.Tile, tilePlacements);
        }
        tilePlacements.Add(placement);

        for (var y = placement.Y; y < placement.Y + placement.Height; y++)
        {
            _occupiedCells.AsSpan(y * mapWidth + placement.X, placement.Width).Fill(true);
        }
    }
}

using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

internal static class FProceduralBorderLayout
{
    public static void Build(IReadOnlyList<FProceduralTilePlacement> placements, FProceduralTileCell[] cells, int width, int height, CancellationToken ct)
    {
        var bordersByBlueprint = new Dictionary<UBlueprintGeneratedClass, Dictionary<string, EDBDDirection>>();
        foreach (var placement in placements)
        {
            ct.ThrowIfCancellationRequested();
            if (placement.Tile.Type is not ETileType.Blocker)
                continue;

            if (!bordersByBlueprint.TryGetValue(placement.Tile.Blueprint, out var borders))
            {
                borders = FProceduralBorderReader.Read(placement.Tile.Blueprint);
                bordersByBlueprint.Add(placement.Tile.Blueprint, borders);
            }

            if (borders.Count == 0)
                continue;

            var activation = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var (name, direction) in borders)
            {
                var offset = placement.TransformOffset(GetOffset(direction));
                var neighborX = placement.X + offset.X;
                var neighborY = placement.Y + offset.Y;
                // BorderTile activates a side only when the neighboring procedural cell is a blocker
                activation[name] = neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height && cells[neighborY * width + neighborX].Type is ETileType.Blocker;
            }
            placement.SpawnerActivation = activation;
        }
    }

    private static (int X, int Y) GetOffset(EDBDDirection direction) => direction switch
    {
        EDBDDirection.Up => (0, -1),
        EDBDDirection.Down => (0, 1),
        EDBDDirection.Left => (-1, 0),
        EDBDDirection.Right => (1, 0),
        EDBDDirection.UpLeft => (-1, -1),
        EDBDDirection.UpRight => (1, -1),
        EDBDDirection.DownLeft => (-1, 1),
        EDBDDirection.DownRight => (1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };
}

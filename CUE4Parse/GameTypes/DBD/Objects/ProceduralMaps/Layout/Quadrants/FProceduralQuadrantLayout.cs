using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Quadrants;

internal readonly record struct FProceduralQuadrant(int X, int Y, int Corner, EQuadrantSpawnType Type, int RotationOffset);

internal sealed class FProceduralQuadrantLayout(FProceduralTileCell[] cells, int width, int height)
{
    private enum ECorner { BottomRight, BottomLeft, TopLeft, TopRight }

    private readonly record struct Corner(int X, int Y, ECorner Index, int RotationOffset = 0)
    {
        public ECorner Next => (ECorner) (((int) Index + 1) & 3);
        public ECorner Previous => (ECorner) (((int) Index + 3) & 3);
        public int Direction => (((int) Index + 1) & 3) * 90;
    }

    private sealed record Boundary(List<Corner> Turns, Dictionary<EDBDDirection, int> SideLengths);

    private readonly bool[] _processedCorners = new bool[cells.Length * 4];

    public static List<FProceduralQuadrant> Build(FProceduralTileCell[] cells, int width, int height, FQuadrantSpawnTypeProperties[] shapes, CancellationToken ct = default)
    {
        var layout = new FProceduralQuadrantLayout(cells, width, height);
        var quadrants = new List<FProceduralQuadrant>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var i = 0; i < 4; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    if (layout.FindQuadrant(new Corner(x, y, (ECorner) i), shapes) is { } quadrant)
                    {
                        quadrants.Add(quadrant);
                    }
                }
            }
        }

        return quadrants;
    }

    private FProceduralQuadrant? FindQuadrant(Corner origin, FQuadrantSpawnTypeProperties[] shapes)
    {
        if (!IsAvailable(origin))
            return null;
        if (TraceBoundary(origin) is not { } boundary)
            return TryCreateLShape(origin);

        Span<Corner> shared = stackalloc Corner[4];
        foreach (var turn in boundary.Turns)
        {
            GetSharedCorners(turn, shared);
            foreach (var corner in shared)
            {
                MarkProcessed(corner);
            }
        }

        var shape = shapes.FirstOrDefault(shape => shape.SectionLenghts.All(section => boundary.SideLengths.GetValueOrDefault(section.Direction) == section.Length));
        if (shape.SectionLenghts == null)
            return null;

        GetSharedCorners(origin, shared);
        foreach (var corner in shared)
        {
            if (Cell(corner.X, corner.Y).Type is not ETileType.Building)
                return new FProceduralQuadrant(corner.X, corner.Y, (int) corner.Index, shape.QuadrantSpawnType, corner.RotationOffset);
        }

        return null;
    }

    private Boundary? TraceBoundary(Corner origin)
    {
        var (x, y) = (origin.X, origin.Y);
        var heading = origin.Direction;
        var nextCorner = origin.Next;
        var turns = new List<Corner>();
        var sideLengths = new Dictionary<EDBDDirection, int>();
        for (var step = 0; step < cells.Length * 4; step++)
        {
            (x, y) = Neighbor(x, y, heading);
            if (!InBounds(x, y))
                return null;

            var corner = new Corner(x, y, nextCorner);
            var canTurn = IsAvailable(corner);

            if (!canTurn && !(HasPath(x, y, heading) && HasPath(x, y, (heading + 180) % 360)))
                return null;

            var side = heading switch
            {
                0 => EDBDDirection.Up,
                90 => EDBDDirection.Right,
                180 => EDBDDirection.Down,
                _ => EDBDDirection.Left
            };

            sideLengths[side] = sideLengths.GetValueOrDefault(side) + 1;

            if (!canTurn)
                continue;

            turns.Add(corner);
            heading = (heading + 90) % 360;
            nextCorner = corner.Next;

            if (x == origin.X && y == origin.Y)
                return new Boundary(turns, sideLengths);
        }

        return null;
    }

    private FProceduralQuadrant? TryCreateLShape(Corner origin)
    {
        if (Cell(origin.X, origin.Y).Type is ETileType.Building)
            return null;

        var (firstX, firstY) = Neighbor(origin.X, origin.Y, origin.Direction);
        var (secondX, secondY) = Neighbor(origin.X, origin.Y, (origin.Direction + 90) % 360);

        var first = new Corner(firstX, firstY, origin.Next);
        var second = new Corner(secondX, secondY, origin.Previous);

        if (!IsAvailable(first) || !IsAvailable(second))
            return null;

        MarkProcessed(first);
        MarkProcessed(second);
        MarkProcessed(origin);

        return new FProceduralQuadrant(origin.X, origin.Y, (int) origin.Index, EQuadrantSpawnType.L_Shape, 0);
    }

    private bool IsAvailable(Corner corner)
    {
        Span<Corner> shared = stackalloc Corner[4];
        GetSharedCorners(corner, shared);

        foreach (var current in shared)
        {
            if (!InBounds(current.X, current.Y) || _processedCorners[CornerIndex(current)])
                return false;
        }

        return HasPath(corner.X, corner.Y, corner.Direction) && HasPath(corner.X, corner.Y, (corner.Direction + 90) % 360);
    }

    private bool HasPath(int x, int y, int direction)
    {
        var cell = Cell(x, y);
        var localDirection = (direction - cell.Rotation + 360) % 360;
        return cell.Path switch
        {
            EPathType.Straight => localDirection is 0 or 180,
            EPathType.DeadEnd => localDirection == 270,
            EPathType.Corner => localDirection is 0 or 270,
            EPathType.Crossroads or EPathType.OrientedCrossroads => true,
            EPathType.TJunction => localDirection is 90 or 180 or 270,
            _ => false
        };
    }

    private void MarkProcessed(Corner corner)
    {
        if (InBounds(corner.X, corner.Y) && Cell(corner.X, corner.Y).Type is not ETileType.Building)
            _processedCorners[CornerIndex(corner)] = true;
    }

    private int CornerIndex(Corner corner) => ((corner.Y * width + corner.X) << 2) + (int) corner.Index;

    private bool InBounds(int x, int y) => (uint) x < (uint) width && (uint) y < (uint) height;

    private FProceduralTileCell Cell(int x, int y) => cells[y * width + x];

    private static (int X, int Y) Neighbor(int x, int y, int direction) => direction switch
    {
        0 => (x, y - 1),
        90 => (x + 1, y),
        180 => (x, y + 1),
        _ => (x - 1, y)
    };

    private static void GetSharedCorners(Corner corner, Span<Corner> result)
    {
        var vertexX = corner.X + (corner.Index is ECorner.BottomRight or ECorner.TopRight ? 1 : 0);
        var vertexY = corner.Y + (corner.Index is ECorner.BottomRight or ECorner.BottomLeft ? 1 : 0);
        for (var i = 0; i < 4; i++)
        {
            var index = (ECorner) (((int) corner.Index + i) & 3);
            result[i] = new Corner(vertexX - (index is ECorner.BottomRight or ECorner.TopRight ? 1 : 0), vertexY - (index is ECorner.BottomRight or ECorner.BottomLeft ? 1 : 0), index, -90 * i);
        }
    }
}

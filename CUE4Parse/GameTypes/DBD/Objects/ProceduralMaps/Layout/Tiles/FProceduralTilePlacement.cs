using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

public sealed record FProceduralTilePlacement(FProceduralTile Tile, int X, int Y, int Flags)
{
    public IReadOnlyDictionary<string, bool>? SpawnerActivation { get; internal set; }
    public IReadOnlyDictionary<string, UBlueprintGeneratedClass>? ComponentVariations { get; internal set; }

    private const int FlipDiagonal = 1;
    private const int FlipVertical = 2;
    private const int FlipHorizontal = 4;
    internal const int QuarterTurnFlags = FlipDiagonal | FlipVertical;

    public int Width => (Flags & FlipDiagonal) != 0 ? Tile.Height : Tile.Width;
    public int Height => (Flags & FlipDiagonal) != 0 ? Tile.Width : Tile.Height;

    internal (int X, int Y) TransformOffset((int X, int Y) offset)
    {
        if ((Flags & FlipDiagonal) != 0) offset = (offset.Y, offset.X);
        if ((Flags & FlipHorizontal) != 0) offset.X = -offset.X;
        if ((Flags & FlipVertical) != 0) offset.Y = -offset.Y;
        return offset;
    }

    public FTransform Transform(FVector2D tileSize, int mapWidth, int mapHeight)
    {
        var tileTransform = UPaperTileLayer.GetTileTransform(Flags);
        var centeredGridX = X + Width / 2f - mapWidth / 2f;
        var centeredGridY = Y + Height / 2f - mapHeight / 2f;
        var translation = new FVector(centeredGridX * tileSize.X, centeredGridY * tileSize.Y, 0);

        // Paper2D uses XZ, DBD lays its procedural tiles on XY
        var rotation = tileTransform.Rotation;
        var scale = tileTransform.Scale3D;
        return new FTransform(new FQuat(rotation.X, -rotation.Z, rotation.Y, rotation.W), translation, new FVector(scale.X, scale.Z, scale.Y));
    }
}

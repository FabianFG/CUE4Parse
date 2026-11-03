using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

public enum ESpriteShapeType : byte
{
    Box,
    Circle,
    Polygon
}

[StructFallback]
public readonly struct FSpriteGeometryShape(FStructFallback fallback)
{
    public readonly ESpriteShapeType ShapeType = fallback.GetOrDefault<ESpriteShapeType>(nameof(ShapeType));
    public readonly FVector2D[] Vertices = fallback.GetOrDefault<FVector2D[]>(nameof(Vertices), []);
    public readonly FVector2D BoxSize = fallback.GetOrDefault<FVector2D>(nameof(BoxSize));
    public readonly FVector2D BoxPosition = fallback.GetOrDefault<FVector2D>(nameof(BoxPosition));
    public readonly float Rotation = fallback.GetOrDefault<float>(nameof(Rotation));
    public readonly bool bNegativeWinding = fallback.GetOrDefault<bool>(nameof(bNegativeWinding));

    public bool IsShapeValid() => ShapeType != ESpriteShapeType.Polygon || Vertices.Length > 2;
}

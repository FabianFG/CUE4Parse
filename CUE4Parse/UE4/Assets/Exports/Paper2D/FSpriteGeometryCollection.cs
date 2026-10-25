using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

public enum ESpritePolygonMode : byte
{
    SourceBoundingBox,
    TightBoundingBox,
    ShrinkWrapped,
    FullyCustom,
    Diced
}

[StructFallback]
public readonly struct FSpriteGeometryCollection
{
    public readonly FSpriteGeometryShape[] Shapes;
    public readonly ESpritePolygonMode GeometryType;
    public readonly int PixelsPerSubdivisionX;
    public readonly int PixelsPerSubdivisionY;
    public readonly bool bAvoidVertexMerging;
    public readonly float AlphaThreshold;
    public readonly float DetailAmount;
    public readonly float SimplifyEpsilon;

    public FSpriteGeometryCollection(FStructFallback fallback) : this(fallback, ESpritePolygonMode.TightBoundingBox) { }

    internal FSpriteGeometryCollection(FStructFallback fallback, ESpritePolygonMode defaultGeometryType)
    {
        Shapes = fallback.GetOrDefault<FSpriteGeometryShape[]>(nameof(Shapes), []);
        GeometryType = fallback.GetOrDefault(nameof(GeometryType), defaultGeometryType);
        PixelsPerSubdivisionX = fallback.GetOrDefault(nameof(PixelsPerSubdivisionX), 32);
        PixelsPerSubdivisionY = fallback.GetOrDefault(nameof(PixelsPerSubdivisionY), 32);
        bAvoidVertexMerging = fallback.GetOrDefault<bool>(nameof(bAvoidVertexMerging));
        AlphaThreshold = fallback.GetOrDefault<float>(nameof(AlphaThreshold));
        DetailAmount = fallback.GetOrDefault(nameof(DetailAmount), 0.5f);
        SimplifyEpsilon = fallback.GetOrDefault(nameof(SimplifyEpsilon), 2f);
    }
}

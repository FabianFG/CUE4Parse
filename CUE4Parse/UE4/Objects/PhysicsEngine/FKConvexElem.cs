using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKConvexElem : FKShapeElem
{
    public FVector[] VertexData;
    public int[] IndexData;
    public FBox ElemBox;
    public FTransform Transform;
    
    public FKConvexElem(FStructFallback fallback) : base(fallback)
    {
        VertexData = fallback.GetOrDefault(nameof(VertexData), Array.Empty<FVector>());
        IndexData = fallback.GetOrDefault(nameof(IndexData), Array.Empty<int>());
        ElemBox = fallback.GetOrDefault<FBox>(nameof(ElemBox));
        Transform = fallback.GetOrDefault<FTransform>(nameof(Transform));
    }
}
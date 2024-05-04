using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKConvexElem
{
    public FVector[] VertexData;
    public int[] IndexData;
    public FBox ElemBox;
    public FTransform Transform;
    public float RestOffset;
    public FName Name;
    public bool bContributeToMass;
    // public ECollisionEnabled CollisionEnabled;
    // TODO add ECollisionEnabled enum?
    
    public FKConvexElem(FStructFallback fallback)
    {
        VertexData = fallback.GetOrDefault(nameof(VertexData), Array.Empty<FVector>());
        IndexData = fallback.GetOrDefault(nameof(IndexData), Array.Empty<int>());
        ElemBox = fallback.GetOrDefault<FBox>(nameof(ElemBox));
        Transform = fallback.GetOrDefault<FTransform>(nameof(Transform));
        RestOffset = fallback.GetOrDefault<float>(nameof(RestOffset));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        bContributeToMass = fallback.GetOrDefault<bool>(nameof(bContributeToMass));
    }
}
using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKAggregateGeom
{
    public FKConvexElem[] ConvexElems;
    
    public FKAggregateGeom(FStructFallback fallback)
    {
        ConvexElems = fallback.GetOrDefault(nameof(ConvexElems), Array.Empty<FKConvexElem>());
    }
}
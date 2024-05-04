using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKAggregateGeom
{
    public FKSphereElem[] SphereElems;
    public FKBoxElem[] BoxElems;
    public FKSphylElem[] SphylElems;
    public FKConvexElem[] ConvexElems;
    public FKTaperedCapsuleElem[] TaperedCapsuleElems;
    // level set elems go here but idk what they are for
    
    public FKAggregateGeom(FStructFallback fallback)
    {
        SphereElems = fallback.GetOrDefault(nameof(SphereElems), Array.Empty<FKSphereElem>());
        BoxElems = fallback.GetOrDefault(nameof(BoxElems), Array.Empty<FKBoxElem>());
        SphylElems = fallback.GetOrDefault(nameof(SphylElems), Array.Empty<FKSphylElem>());
        ConvexElems = fallback.GetOrDefault(nameof(ConvexElems), Array.Empty<FKConvexElem>());
        TaperedCapsuleElems = fallback.GetOrDefault(nameof(TaperedCapsuleElems), Array.Empty<FKTaperedCapsuleElem>());
    }
}
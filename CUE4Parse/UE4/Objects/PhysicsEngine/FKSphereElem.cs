using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKSphereElem : FKShapeElem
{
    public FVector Center;
    public float Radius;
    
    public FKSphereElem(FStructFallback fallback) : base(fallback)
    {
        Center = fallback.GetOrDefault(nameof(Center), FVector.ZeroVector);
        Radius = fallback.GetOrDefault<float>(nameof(Radius));
    }
}
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKSphylElem : FKShapeElem
{
    public FVector Center;
    public FRotator Rotation;
    public float Radius;
    public float Length;
    
    public FKSphylElem(FStructFallback fallback) : base(fallback)
    {
        Center = fallback.GetOrDefault(nameof(Center), FVector.ZeroVector);
        Rotation = fallback.GetOrDefault(nameof(Rotation), FRotator.ZeroRotator);
        Radius = fallback.GetOrDefault<float>(nameof(Radius));
        Length = fallback.GetOrDefault<float>(nameof(Length));
    }
}
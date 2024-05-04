using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKTaperedCapsuleElem : FKShapeElem
{
    public FVector Center;
    public FRotator Rotation;
    public float Radius0;
    public float Radius1;
    public float Length;
    
    public FKTaperedCapsuleElem(FStructFallback fallback) : base(fallback)
    {
        Center = fallback.GetOrDefault(nameof(Center), FVector.ZeroVector);
        Rotation = fallback.GetOrDefault(nameof(Rotation), FRotator.ZeroRotator);
        Radius0 = fallback.GetOrDefault<float>(nameof(Radius0));
        Radius1 = fallback.GetOrDefault<float>(nameof(Radius1));
        Length = fallback.GetOrDefault<float>(nameof(Length));
    }
}

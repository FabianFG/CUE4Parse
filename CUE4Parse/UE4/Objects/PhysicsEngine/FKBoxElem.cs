using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKBoxElem : FKShapeElem
{
    public FVector Center;
    public FRotator Rotation;
    public float X;
    public float Y;
    public float Z;
    
    public FKBoxElem(FStructFallback fallback) : base(fallback)
    {
        Center = fallback.GetOrDefault(nameof(Center), FVector.ZeroVector);
        Rotation = fallback.GetOrDefault(nameof(Rotation), FRotator.ZeroRotator);
        X = fallback.GetOrDefault<float>(nameof(X));
        Y = fallback.GetOrDefault<float>(nameof(Y));
        Z = fallback.GetOrDefault<float>(nameof(Z));
    }
}
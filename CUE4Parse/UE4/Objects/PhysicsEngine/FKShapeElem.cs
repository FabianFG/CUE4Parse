using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FKShapeElem
{
    public float RestOffset;
    public FName Name;
    public bool bContributeToMass;
    public ECollisionEnabled CollisionEnabled;
    
    public FKShapeElem(FStructFallback fallback)
    {
        RestOffset = fallback.GetOrDefault<float>(nameof(RestOffset));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        bContributeToMass = fallback.GetOrDefault<bool>(nameof(bContributeToMass));
        CollisionEnabled = fallback.GetOrDefault<ECollisionEnabled>(nameof(CollisionEnabled));
    }
}

public enum ECollisionEnabled
{
    NoCollision,
    QueryOnly,
    PhysicsOnly,
    QueryAndPhysics,
}
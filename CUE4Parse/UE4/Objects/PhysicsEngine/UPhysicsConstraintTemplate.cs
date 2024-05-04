using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public class UPhysicsConstraintTemplate : Assets.Exports.UObject
{
    public FConstraintInstance DefaultInstance;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DefaultInstance = GetOrDefault<FConstraintInstance>(nameof(DefaultInstance));
    }
}
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public class UBodySetupCore : Assets.Exports.UObject
{
    public FName? BoneName { get; private set; }
    public EPhysicsType PhysicsType { get; private set; } = EPhysicsType.PhysType_Default;
    public ECollisionTraceFlag CollisionTraceFlag { get; private set; } = ECollisionTraceFlag.CTF_UseDefault;
    public EBodyCollisionResponse CollisionReponse { get; protected set; } = EBodyCollisionResponse.BodyCollision_Enabled;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BoneName = GetOrDefault<FName?>(nameof(BoneName));
        PhysicsType = GetOrDefault(nameof(PhysicsType), PhysicsType);
        CollisionTraceFlag = GetOrDefault(nameof(CollisionTraceFlag), CollisionTraceFlag);
        CollisionReponse = GetOrDefault(nameof(CollisionReponse), CollisionReponse);
    }
}

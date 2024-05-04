using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public class USkeletalBodySetup : UBodySetup
{
    public FName BoneName;
    public EPhysicsType PhysicsType;
    public ECollisionTraceFlag CollisionTraceFlag;
    // TODO public FBodyInstance DefaultInstance;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BoneName = GetOrDefault<FName>(nameof(BoneName));
        PhysicsType = GetOrDefault<EPhysicsType>(nameof(PhysicsType));
        CollisionTraceFlag = GetOrDefault<ECollisionTraceFlag>(nameof(CollisionTraceFlag));
    }
}

public enum EPhysicsType
{
    PhysType_Default,
    PhysType_Kinematic,
    PhysType_Simulated
}

public enum ECollisionTraceFlag
{
    CTF_UseDefault,
    CTF_UseSimpleAndComplex,
    CTF_UseSimpleAsComplex,
    CTF_UseComplexAsSimple,
    CTF_MAX
}
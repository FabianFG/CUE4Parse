using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class USkeletalMeshComponentBudgeted : USkeletalMeshComponent;

public class USkeletalMeshComponent : USkinnedMeshComponent
{
    public FSingleAnimationPlayData? AnimationData { get; private set; }
    public FPackageIndex? BodySetup { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        AnimationData = GetOrDefault<FSingleAnimationPlayData?>(nameof(AnimationData));

        var bEnablePerPolyCollision = GetOrDefault<bool>("bEnablePerPolyCollision");
        if (Ar.Ver < EUnrealEngineObjectUE4Version.REMOVE_SKELETALMESH_COMPONENT_BODYSETUP_SERIALIZATION && bEnablePerPolyCollision)
        {
            BodySetup = new FPackageIndex(Ar);
        }

        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 20;
    }

    public override UBodySetup? GetBodySetup() => BodySetup?.Load<UBodySetup>();
}

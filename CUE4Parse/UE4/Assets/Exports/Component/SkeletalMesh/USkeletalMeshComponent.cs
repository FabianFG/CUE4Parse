using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class USkeletalMeshComponentBudgeted : USkeletalMeshComponent;

public class USkeletalMeshComponent : USkinnedMeshComponent
{
    public FSingleAnimationPlayData? AnimationData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        AnimationData = GetOrDefault<FSingleAnimationPlayData>(nameof(AnimationData));

        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 20;
    }
}

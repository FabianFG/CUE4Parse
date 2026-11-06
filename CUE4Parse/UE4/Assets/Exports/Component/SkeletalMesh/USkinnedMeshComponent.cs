using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class USkinnedMeshComponent : UMeshComponent
{
    public FPackageIndex? LeaderPoseComponent { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LeaderPoseComponent = GetOrDefault(nameof(LeaderPoseComponent), GetOrDefault<FPackageIndex?>("MasterPoseComponent"));
    }

    public FPackageIndex GetSkeletalMesh()
    {
        // SkeletalMesh was deprecated in 5.1
        TryGetValue(out FPackageIndex mesh, "SkeletalMesh", "SkinnedAsset");
        return mesh ?? new FPackageIndex();
    }

    public FPackageIndex GetSkeletalMesh(string parameterName)
    {
        TryGetValue(out FPackageIndex mesh, parameterName);
        return mesh ?? new FPackageIndex();
    }

    public bool SetSkeletalMeshIfNull(FPackageIndex mesh)
    {
        if (GetSkeletalMesh().IsNull)
        {
            SetSkeletalMesh(mesh);
            return true;
        }
        return false;
    }

    public void SetSkeletalMesh(FPackageIndex mesh)
    {
        PropertyUtil.Set(this, "SkeletalMesh", mesh);
    }
}

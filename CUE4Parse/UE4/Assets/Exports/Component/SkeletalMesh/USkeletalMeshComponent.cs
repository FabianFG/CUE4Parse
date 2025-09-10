using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class USkeletalMeshComponentBudgeted : USkeletalMeshComponent;

public class USkeletalMeshComponent : USkinnedMeshComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 20;
    }

    public FPackageIndex GetSkeletalMesh()
    {
        var skeletalMesh = GetSkeletalMesh("SkeletalMesh"); // deprecated in 5.1 so fallback below
        if (skeletalMesh.IsNull) skeletalMesh = GetSkeletalMesh("SkinnedAsset");

        return skeletalMesh;
    }

    public FPackageIndex GetSkeletalMesh(string parameterName)
    {
        var mesh = new FPackageIndex();
        var current = this;
        while (true)
        {
            if (current is null) break;
            mesh = current.GetOrDefault(parameterName, new FPackageIndex());
            if (!mesh.IsNull || current.Template == null)
                break;
            current = current.Template.Load<USkeletalMeshComponent>();
        }

        return mesh;
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

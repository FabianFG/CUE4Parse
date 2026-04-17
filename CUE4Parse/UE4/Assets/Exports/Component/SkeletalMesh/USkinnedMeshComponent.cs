using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

public class USkinnedMeshComponent : UMeshComponent
{
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

    public override IEnumerable<UObject> GetExportableReferences()
    {
        if (GetSkeletalMesh().TryLoad<USkeletalMesh>(out var mesh))
            yield return mesh;

        foreach (var obj in base.GetExportableReferences())
            yield return obj;
    }
}

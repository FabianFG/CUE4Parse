using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UMeshComponent : UPrimitiveComponent
{
    public FPackageIndex?[] OverrideMaterials = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        OverrideMaterials = GetOrDefault(nameof(OverrideMaterials), OverrideMaterials);
    }

    public override IEnumerable<UObject> GetExportableReferences()
    {
        foreach (var overrideMaterial in OverrideMaterials)
        {
            if (overrideMaterial?.TryLoad<UMaterialInterface>(out var material) == true)
                yield return material;
        }

        foreach (var obj in base.GetExportableReferences())
            yield return obj;
    }
}

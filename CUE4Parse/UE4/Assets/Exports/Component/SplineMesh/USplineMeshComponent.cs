using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

public class USplineMeshComponent : UStaticMeshComponent
{
    public FSplineMeshParams SplineParams;
    public FGuid CachedMeshBodySetupGuid;
    public FPackageIndex? StaticMesh;
    public FPackageIndex[] OverrideMaterials;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CachedMeshBodySetupGuid = GetOrDefault<FGuid>(nameof(CachedMeshBodySetupGuid));
        StaticMesh = GetOrDefault<FPackageIndex>(nameof(StaticMesh));

        if (TryGetValue(out FStructFallback fallback, nameof(SplineParams)))
            SplineParams = new FSplineMeshParams(fallback);

        OverrideMaterials = GetOrDefault<FPackageIndex[]>(nameof(OverrideMaterials), []);
    }
}

using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public abstract class UStreamableRenderAsset : UObject;

public abstract class USkinnedAsset : UStreamableRenderAsset
{
    public FBoxSphereBounds? Bounds { get; protected set; }
    public FSkeletalMaterial[] SkeletalMaterials { get; protected set; }
    public FReferenceSkeleton ReferenceSkeleton { get; protected set; }
    public FSkeletalMeshLODGroupSettings[]? LODInfo { get; protected set; }
    public FStaticLODModel[]? LODModels { get; protected set; }
    public FPackageIndex[] Sockets { get; protected set; } = [];
    public FPackageIndex[] MorphTargets { get; protected set; } = [];
    public bool bHasVertexColors { get; protected set; }
    public FPackageIndex Skeleton { get; protected set; }
    public FPackageIndex?[] Materials { get; protected set; } = []; // UMaterialInterface[]
    public FPackageIndex PhysicsAsset { get; protected set; }
    public FPackageIndex[]? AssetUserData { get; protected set; }
    public FNaniteResources? NaniteResources { get; protected set; }

    public virtual void PopulateMorphTargetVerticesData() { }
}

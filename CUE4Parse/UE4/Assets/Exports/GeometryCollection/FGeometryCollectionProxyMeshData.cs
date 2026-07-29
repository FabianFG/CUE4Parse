using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

[StructFallback]
public readonly struct FGeometryCollectionProxyMeshData : IUStruct
{
    public readonly FPackageIndex[] ProxyMeshes;
    public readonly FTransform[]? MeshTransforms;
    public readonly FGeometryCollectionProxyMeshMaterials[]? MeshOverrideMaterials;

    public FGeometryCollectionProxyMeshData(FStructFallback fallback)
    {
        ProxyMeshes = fallback.GetOrDefault<FPackageIndex[]>(nameof(ProxyMeshes), []);
        MeshTransforms = fallback.GetOrDefault<FTransform[]?>(nameof(MeshTransforms));
        MeshOverrideMaterials = fallback.GetOrDefault<FGeometryCollectionProxyMeshMaterials[]?>(nameof(MeshOverrideMaterials));
    }
}
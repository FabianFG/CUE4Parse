using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

[StructFallback]
public readonly struct FGeometryCollectionProxyMeshData
{
    public readonly FPackageIndex[] ProxyMeshes;

    public FGeometryCollectionProxyMeshData(FStructFallback fallback)
    {
        ProxyMeshes = fallback.GetOrDefault<FPackageIndex[]>(nameof(ProxyMeshes), []);
    }
}

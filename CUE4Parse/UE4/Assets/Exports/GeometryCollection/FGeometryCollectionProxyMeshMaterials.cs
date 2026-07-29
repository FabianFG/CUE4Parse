using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

[StructFallback]
public readonly struct FGeometryCollectionProxyMeshMaterials : IUStruct
{
    public readonly FPackageIndex[] Materials;
    
    public FGeometryCollectionProxyMeshMaterials(FStructFallback fallback)
    {
        Materials = fallback.GetOrDefault<FPackageIndex[]>(nameof(Materials), []);
    }
}
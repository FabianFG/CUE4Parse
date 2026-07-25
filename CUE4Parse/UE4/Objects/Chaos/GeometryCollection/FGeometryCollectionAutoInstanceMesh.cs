using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

[StructFallback]
public readonly struct FGeometryCollectionAutoInstanceMesh : IUStruct
{
    public readonly FPackageIndex? Mesh;
    public readonly FPackageIndex?[]? Materials;
    public readonly int NumInstances;
    public readonly float[]? CustomData;

    public FGeometryCollectionAutoInstanceMesh(FStructFallback fallback)
    {
        Mesh = fallback.GetOrDefault<FPackageIndex?>(nameof(Mesh));
        Materials = fallback.GetOrDefault<FPackageIndex?[]?>(nameof(Materials));
        NumInstances = fallback.GetOrDefault<int>(nameof(NumInstances));
        CustomData = fallback.GetOrDefault<float[]?>(nameof(CustomData));
    }
}

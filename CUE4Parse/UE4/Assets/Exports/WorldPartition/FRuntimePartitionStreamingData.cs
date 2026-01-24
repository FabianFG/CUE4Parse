using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FRuntimePartitionStreamingData : IUStruct
{
    public readonly FName Name;
    public readonly int LoadingRange;
    public readonly FPackageIndex[] SpatiallyLoadedCells;
    public readonly FPackageIndex[] NonSpatiallyLoadedCells;

    public FRuntimePartitionStreamingData(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        LoadingRange = fallback.GetOrDefault(nameof(LoadingRange), 0);
        SpatiallyLoadedCells = fallback.GetOrDefault(nameof(SpatiallyLoadedCells), fallback.GetOrDefault<FPackageIndex[]>("StreamingCells", []));
        NonSpatiallyLoadedCells = fallback.GetOrDefault(nameof(NonSpatiallyLoadedCells), fallback.GetOrDefault<FPackageIndex[]>("NonStreamingCells", []));
    }
}

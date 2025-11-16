using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FRuntimePartitionHLODSetup : IUStruct
{
    public readonly FName Name;
    public readonly FPackageIndex PartitionLayer;
    public readonly bool bIsSpatiallyLoaded;
    
    public FRuntimePartitionHLODSetup(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        PartitionLayer = fallback.GetOrDefault(nameof(PartitionLayer), new FPackageIndex());
        bIsSpatiallyLoaded = fallback.GetOrDefault(nameof(bIsSpatiallyLoaded), true);
    }
}
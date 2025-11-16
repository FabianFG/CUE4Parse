using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FRuntimePartitionDesc : IUStruct
{
    public readonly FName Name;
    public readonly FPackageIndex Class;
    public readonly FPackageIndex MainLayer;
    public readonly FPackageIndex[] HLODSetups;
    
    public FRuntimePartitionDesc(FStructFallback fallback)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        Class = fallback.GetOrDefault(nameof(Class), new FPackageIndex());
        MainLayer = fallback.GetOrDefault(nameof(MainLayer), new FPackageIndex());
        HLODSetups = fallback.GetOrDefault<FPackageIndex[]>(nameof(HLODSetups), []);
    }
}
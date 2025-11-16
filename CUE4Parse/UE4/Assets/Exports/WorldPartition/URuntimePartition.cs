using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class URuntimePartition : UObject
{
    public FName Name;
    public bool bBlockOnSlowStreaming;
    public bool bClientOnlyVisible;
    public int Priority;
    public ERuntimePartitionCellBoundsMethod BoundsMethod;
    public int LoadingRange;
    public FLinearColor DebugColor;
    public int HLODIndex;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        Name = GetOrDefault<FName>(nameof(Name));
        bBlockOnSlowStreaming = GetOrDefault(nameof(bBlockOnSlowStreaming), false);
        bClientOnlyVisible = GetOrDefault(nameof(bClientOnlyVisible), false);
        Priority = GetOrDefault<int>(nameof(Priority));
        BoundsMethod = GetOrDefault<ERuntimePartitionCellBoundsMethod>(nameof(BoundsMethod));
        LoadingRange = GetOrDefault<int>(nameof(LoadingRange));
        DebugColor = GetOrDefault<FLinearColor>(nameof(DebugColor));
        HLODIndex = GetOrDefault<int>(nameof(HLODIndex));
    }
}

public class URuntimePartitionLHGrid : URuntimePartition;
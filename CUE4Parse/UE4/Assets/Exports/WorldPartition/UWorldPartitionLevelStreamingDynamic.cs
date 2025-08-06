using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionLevelStreamingDynamic : ULevelStreamingDynamic
{
    public FPackageIndex? StreamingCell;
    public FPackageIndex? OuterWorldPartition;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        StreamingCell = GetOrDefault<FPackageIndex>(nameof(StreamingCell));
        OuterWorldPartition = GetOrDefault<FPackageIndex>(nameof(OuterWorldPartition));
    }
}
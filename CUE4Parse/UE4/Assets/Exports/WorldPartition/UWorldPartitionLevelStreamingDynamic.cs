using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionLevelStreamingDynamic : ULevelStreamingDynamic
{
    public FPackageIndex? StreamingCell;
    public FSoftObjectPath? OuterWorldPartition;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        StreamingCell = GetOrDefault<FPackageIndex>(nameof(StreamingCell));
        // TOptional<SoftObject> in 5.4+, WeakObject/Object before
        OuterWorldPartition = GetOrDefault<FSoftObjectPath>(nameof(OuterWorldPartition));
    }
}

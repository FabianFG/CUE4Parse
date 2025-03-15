using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionRuntimeCellData : UObject
{
    public string DebugName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DebugName = Ar.ReadFString();
    }
}

public class UWorldPartitionRuntimeCell : UObject { }

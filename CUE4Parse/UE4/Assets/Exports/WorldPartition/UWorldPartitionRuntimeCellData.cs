using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionRuntimeCellData : UObject
{
    public string? DebugName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DebugName = Ar.Game >= EGame.GAME_UE5_3 ? Ar.ReadFString() : null;
    }
}

public class UWorldPartitionRuntimeCell : UObject;

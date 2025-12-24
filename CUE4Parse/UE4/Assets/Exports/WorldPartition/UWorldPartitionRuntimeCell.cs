using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionRuntimeCell : UObject
{
    public bool bIsSpatiallyLoaded;
    public FDataLayerInstanceNames? DataLayers;
    public FLinearColor CellDebugColor;
    public FPackageIndex? RuntimeCellData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        bIsSpatiallyLoaded = GetOrDefault<bool>(nameof(bIsSpatiallyLoaded));
        DataLayers = GetOrDefault<FDataLayerInstanceNames?>(nameof(DataLayers));
        CellDebugColor = GetOrDefault<FLinearColor>(nameof(CellDebugColor));
        RuntimeCellData = GetOrDefault<FPackageIndex>(nameof(RuntimeCellData));
    }
}

public class UWorldPartitionRuntimeLevelStreamingCell : UWorldPartitionRuntimeCell
{
    public FPackageIndex? LevelStreaming;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        LevelStreaming = GetOrDefault<FPackageIndex>(nameof(LevelStreaming));
    }
}

public class UWorldPartitionRuntimeCellData : UObject
{
    public FBox ContentBounds;
    public FBox? CellBounds;
    public FName GridName;
    public int Priority;
    public int HierarchicalLevel;
    public string? DebugName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ContentBounds = GetOrDefault<FBox>(nameof(ContentBounds));
        CellBounds = GetOrDefault<FBox?>(nameof(CellBounds));
        GridName = GetOrDefault<FName>(nameof(GridName));
        Priority = GetOrDefault<int>(nameof(Priority));
        HierarchicalLevel = GetOrDefault<int>(nameof(HierarchicalLevel));

        DebugName = Ar.Game >= EGame.GAME_UE5_3 ? Ar.ReadFString() : null;
    }
}

public class UWorldPartitionRuntimeCellDataHashSet : UWorldPartitionRuntimeCellData
{
    public bool bIs2D;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        bIs2D = GetOrDefault(nameof(bIs2D), false);
    }
}

public class UWorldPartitionRuntimeCellDataSpatialHash : UWorldPartitionRuntimeCellData
{
    public FVector Position;
    public float Extent;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Position = GetOrDefault<FVector>(nameof(Position));
        Extent = GetOrDefault<float>(nameof(Extent));
    }
}

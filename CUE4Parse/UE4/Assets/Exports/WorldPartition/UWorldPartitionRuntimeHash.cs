﻿using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartitionRuntimeHash : UObject;

public class UWorldPartitionRuntimeHashSet : UWorldPartitionRuntimeHash
{
    public FRuntimePartitionStreamingData[] RuntimeStreamingData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        RuntimeStreamingData = GetOrDefault<FRuntimePartitionStreamingData[]>(nameof(RuntimeStreamingData), []);
    }
}

public class UWorldPartitionRuntimeSpatialHash : UWorldPartitionRuntimeHash
{
    public FSpatialHashStreamingGrid[] StreamingGrids;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        StreamingGrids = GetOrDefault<FSpatialHashStreamingGrid[]>(nameof(StreamingGrids), []);
    }
}
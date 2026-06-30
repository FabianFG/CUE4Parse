using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.WorldPartition;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public class WorldSettingsDto : ActorDto
{
    internal WorldSettingsDto(AWorldSettings worldSettings) : base(worldSettings)
    {
        RootComponent = new SceneComponentDto(FTransform.Identity, "SceneComponent", this);

        if (worldSettings.WorldPartition.TryLoad<UWorldPartition>(out var partition) &&
            partition.RuntimeHash?.TryLoad<UWorldPartitionRuntimeHash>(out var runtimeHash) == true)
        {
            RootComponent.AttachedActors.Add(new RuntimeHashActorDto(runtimeHash));
        }
    }
}

public class RuntimeHashActorDto : ActorDto
{
    internal RuntimeHashActorDto(UWorldPartitionRuntimeHash runtimeHash) : base(runtimeHash)
    {
        RootComponent = new SceneComponentDto(FTransform.Identity, "SceneComponent", this);

        switch (runtimeHash)
        {
            case UWorldPartitionRuntimeHashSet set:
            {
                foreach (var streamingData in set.RuntimeStreamingData.OrderBy(x => x.LoadingRange))
                {
                    var component = new SceneComponentDto(FTransform.Identity, streamingData.Name.ToString(), this);

                    Process(streamingData.SpatiallyLoadedCells, component);
                    Process(streamingData.NonSpatiallyLoadedCells, component, true);

                    RootComponent?.Children.Add(component);
                }
                break;
            }
            case UWorldPartitionRuntimeSpatialHash spatial:
            {
                foreach (var grid in spatial.StreamingGrids)
                {
                    var component = new SceneComponentDto(/*new FTransform(grid.Origin)*/FTransform.Identity, grid.GridName.ToString(), this);

                    foreach (var level in grid.GridLevels)
                    foreach (var cell in level.LayerCells)
                    {
                        Process(cell.GridCells, component);
                    }

                    RootComponent?.Children.Add(component);
                }
                break;
            }
        }
    }

    private void Process(FPackageIndex[] ptrs, SceneComponentDto component, bool isNonSpatiallyLoaded = false)
    {
        foreach (var ptr in ptrs)
        {
            if (!ptr.TryLoad<UWorldPartitionRuntimeCell>(out var runtimeCell)) continue;

            component.AttachedActors.Add(new RuntimeCellActorDto(runtimeCell));
        }
    }
}

public class RuntimeCellActorDto : ActorDto
{
    internal RuntimeCellActorDto(UWorldPartitionRuntimeCell runtimeCell) : base(runtimeCell)
    {
        // var dataLayers = runtimeCell.DataLayers?.DataLayers.Select(x => x.Text).ToArray() ?? [];

        // var center = FVector.ZeroVector;
        // if (runtimeCell.RuntimeCellData?.TryLoad<UWorldPartitionRuntimeCellData>(out var data) == true)
        // {
        //     if (data is UWorldPartitionRuntimeCellDataSpatialHash spatial && spatial.Position != FVector.ZeroVector)
        //     {
        //         center = spatial.Position;
        //     }
        //     else
        //     {
        //         var box = data.CellBounds ?? data.ContentBounds;
        //         box.GetCenterAndExtents(out center, out _);
        //     }
        // }

        // all this transform thing is useful to place objects within the bounds of the world
        // but the world already contains that data in its meshes transform, so we should not apply any additional offset to the world itself
        RootComponent = new SceneComponentDto(/*new FTransform(center)*/FTransform.Identity, "SceneComponent", this);

        if (runtimeCell is UWorldPartitionRuntimeLevelStreamingCell streaming &&
            streaming.LevelStreaming?.TryLoad<ULevelStreaming>(out var level) == true &&
            level.WorldAsset?.TryLoad<UWorld>(out var w) == true)
        {
            StreamingLevels = [new StreamingLevel(w, level)];
        }
    }
}

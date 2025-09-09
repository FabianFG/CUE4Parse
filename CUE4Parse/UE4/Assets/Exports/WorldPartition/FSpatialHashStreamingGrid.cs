using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FSpatialHashStreamingGrid : IUStruct
{
    public readonly FName GridName;
    public readonly FVector Origin;
    public readonly int CellSize;
    public readonly float LoadingRange;
    public readonly bool bBlockOnSlowStreaming;
    public readonly FLinearColor DebugColor;
    public readonly FSpatialHashStreamingGridLevel[] GridLevels;
    public readonly FBox WorldBounds;
    public readonly bool bClientOnlyVisible;
    public readonly int GridIndex;
    // public readonly FSpatialHashSettings Settings;
    
    public FSpatialHashStreamingGrid(FStructFallback fallback)
    {
        GridName = fallback.GetOrDefault<FName>(nameof(GridName));
        Origin = fallback.GetOrDefault<FVector>(nameof(Origin));
        CellSize = fallback.GetOrDefault(nameof(CellSize), 0);
        LoadingRange = fallback.GetOrDefault(nameof(LoadingRange), 0f);
        bBlockOnSlowStreaming = fallback.GetOrDefault(nameof(bBlockOnSlowStreaming), false);
        DebugColor = fallback.GetOrDefault<FLinearColor>(nameof(DebugColor));
        GridLevels = fallback.GetOrDefault<FSpatialHashStreamingGridLevel[]>(nameof(GridLevels), []);
        WorldBounds = fallback.GetOrDefault<FBox>(nameof(WorldBounds));
        bClientOnlyVisible = fallback.GetOrDefault(nameof(bClientOnlyVisible), false);
        GridIndex = fallback.GetOrDefault(nameof(GridIndex), 0);
        // Settings = fallback.GetOrDefault<FSpatialHashSettings>(nameof(Settings));
    }
}
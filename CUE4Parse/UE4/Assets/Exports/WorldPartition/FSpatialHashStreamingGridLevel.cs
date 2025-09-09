using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FSpatialHashStreamingGridLevel : IUStruct
{
    public readonly FSpatialHashStreamingGridLayerCell[] LayerCells;
    // public readonly Dictionary<long, int> LayerCellsMapping;
    
    public FSpatialHashStreamingGridLevel(FStructFallback fallback)
    {
        LayerCells = fallback.GetOrDefault<FSpatialHashStreamingGridLayerCell[]>(nameof(LayerCells), []);
        // LayerCellsMapping = fallback.GetOrDefault<Dictionary<long, int>>(nameof(LayerCellsMapping), []);
    }
}
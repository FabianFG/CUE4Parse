using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FSpatialHashStreamingGridLayerCell : IUStruct
{
    public readonly FPackageIndex[] GridCells;
    
    public FSpatialHashStreamingGridLayerCell(FStructFallback fallback)
    {
        GridCells = fallback.GetOrDefault<FPackageIndex[]>(nameof(GridCells), []);
    }
}
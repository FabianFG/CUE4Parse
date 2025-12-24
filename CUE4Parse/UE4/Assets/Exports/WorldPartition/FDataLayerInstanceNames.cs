using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

[StructFallback]
public readonly struct FDataLayerInstanceNames : IUStruct
{
    public readonly bool bIsFirstDataLayerExternal;
    public readonly FName[] DataLayers;

    public FDataLayerInstanceNames(FStructFallback fallback)
    {
        bIsFirstDataLayerExternal = fallback.GetOrDefault<bool>(nameof(bIsFirstDataLayerExternal));
        DataLayers = fallback.GetOrDefault<FName[]>(nameof(DataLayers), []);
    }
}

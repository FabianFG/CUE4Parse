using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;

public class UDataLayerAsset : UDataAsset
{
    public EDataLayerType DataLayerType;
    public bool bSupportsActorFilters;
    public FColor DebugColor;
    public EDataLayerLoadFilter LoadFilter;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DataLayerType = GetOrDefault<EDataLayerType>(nameof(DataLayerType));
        bSupportsActorFilters = GetOrDefault<bool>(nameof(bSupportsActorFilters));
        DebugColor = GetOrDefault<FColor>(nameof(DebugColor));
        LoadFilter = GetOrDefault<EDataLayerLoadFilter>(nameof(LoadFilter));
    }
}

using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;

public class UDataLayerInstanceWithAsset : UDataLayerInstance
{
    public FPackageIndex DataLayerAsset;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DataLayerAsset = GetOrDefault(nameof(DataLayerAsset), new FPackageIndex());
    }
}

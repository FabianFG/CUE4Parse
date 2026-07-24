using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UGeometryCollectionComponent : UMeshComponent
{
    public FPackageIndex? RestCollection;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        RestCollection = GetOrDefault<FPackageIndex?>(nameof(RestCollection));
    }
}

using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OuterWorlds2.Objects;

public class FPropertryDataObjectContainer
{
    public FPackageIndex[] ObjectStore;
    public FSoftObjectPath[] SoftObjectPathStore;
    public FPropertryDataObjectContainer(FAssetArchive Ar)
    {
        ObjectStore = Ar.ReadArray(() => new FPackageIndex(Ar));
        SoftObjectPathStore = Ar.ReadArray(() => new FSoftObjectPath(Ar));
    }
}

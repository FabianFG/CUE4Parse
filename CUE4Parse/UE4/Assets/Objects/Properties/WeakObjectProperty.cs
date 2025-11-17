using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public class WeakObjectProperty : ObjectProperty
{
    public WeakObjectProperty(FPackageIndex value) : base(value) { }

    public WeakObjectProperty(FAssetArchive Ar, ReadType type) : base(Ar, type) { }
}

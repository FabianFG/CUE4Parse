using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public class ClassProperty : ObjectProperty
{
    public ClassProperty(FPackageIndex value) : base(value) { }

    public ClassProperty(FAssetArchive Ar, ReadType type) : base(Ar, type) { }
}

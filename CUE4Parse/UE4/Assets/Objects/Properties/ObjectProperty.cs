using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class ObjectProperty : FPropertyTagType<FPackageIndex>
    {
        public ObjectProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FPackageIndex(Ar, 0),
                _ => new FPackageIndex(Ar)
            };
        }
    }
}
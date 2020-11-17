using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class NameProperty : FPropertyTagType<FName>
    {
        public NameProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FName(),
                _ => Ar.ReadFName()
            };
        }
    }
}
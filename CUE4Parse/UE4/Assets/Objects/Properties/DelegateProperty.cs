using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class DelegateProperty : FPropertyTagType<FName>
    {
        public readonly int Num;

        public DelegateProperty(FAssetArchive Ar, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Num = 0;
                Value = new FName();
            }
            else
            {
                Num = Ar.Read<int>();
                Value = Ar.ReadFName();    
            }
        }
    }
}
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FNumberedPair
    {
        public readonly FName Key;
        public readonly FValueId Value;

        public FNumberedPair(FAssetRegistryReader Ar)
        {
            Key = Ar.ReadFName();
            Value = new FValueId(Ar);
        }

        public FNumberedPair(FName key, FValueId value)
        {
            Key = key;
            Value = value;
        }
    }
}
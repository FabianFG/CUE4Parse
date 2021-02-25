namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FNumberlessPair
    {
        public readonly uint Key;
        public readonly FValueId Value;

        public FNumberlessPair(FAssetRegistryReader Ar)
        {
            Key = Ar.Read<uint>();
            Value = new FValueId(Ar);
        }
    }
}
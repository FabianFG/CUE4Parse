using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class FFieldPath : FField
    {
        public FFieldPath(FAssetArchive Ar)
        {
            base.Deserialize(Ar);
        }
    }
}
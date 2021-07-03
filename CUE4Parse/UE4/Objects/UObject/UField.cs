using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UField : Assets.Exports.UObject
    {
        /** Next Field in the linked list */
        public FPackageIndex Next; // UField

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.RemoveUField_Next)
            {
                Next = new FPackageIndex(Ar);
            }
        }
    }
}
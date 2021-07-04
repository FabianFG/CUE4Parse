using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FIntBulkData : FByteBulkData
    {
        public FIntBulkData(FAssetArchive Ar) : base(Ar, true)
        {
        }
    }
}
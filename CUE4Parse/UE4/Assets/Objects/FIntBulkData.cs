using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Objects;

public class FIntBulkData : FByteBulkData
{
    public FIntBulkData(FAssetArchive Ar) : base(Ar, true)
    {
    }
}

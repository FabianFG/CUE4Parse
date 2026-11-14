using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Objects;

public class FIntBulkData : TBulkData<int>
{
    public FIntBulkData(int[] data) : base(data) { }
    public FIntBulkData(FAssetArchive Ar) : base(Ar) { }
}

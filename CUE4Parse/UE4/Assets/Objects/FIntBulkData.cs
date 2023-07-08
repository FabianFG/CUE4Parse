using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Objects;

public class FIntBulkData(FAssetArchive Ar) : FByteBulkData(Ar, true);

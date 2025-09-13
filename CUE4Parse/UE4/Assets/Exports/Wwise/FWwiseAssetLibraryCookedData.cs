using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class FWwiseAssetLibraryCookedData : FStructFallback
{
    public FWwisePackagedFile[] PackagedFiles;

    public FWwiseAssetLibraryCookedData(FAssetArchive Ar) : base(Ar, "WwiseAssetLibraryCookedData")
    {
        PackagedFiles = Ar.ReadArray(() => new FWwisePackagedFile(Ar));
        foreach (var packagedFile in PackagedFiles)
        {
            packagedFile.SerializeBulkData(Ar);
        }
    }
}

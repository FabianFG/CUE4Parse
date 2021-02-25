using CUE4Parse.UE4.AssetRegistry.Readers;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetBundleData
    {
        public readonly FAssetBundleEntry[] Bundles;

        public FAssetBundleData()
        {
            Bundles = new FAssetBundleEntry[0];
        }
        
        public FAssetBundleData(FAssetRegistryArchive Ar)
        {
            Bundles = Ar.ReadArray(() => new FAssetBundleEntry(Ar));
        }
    }
}
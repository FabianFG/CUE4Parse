using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetBundleEntry
    {
        public readonly FName BundleName;
        public readonly FSoftObjectPath[] BundleAssets;

        public FAssetBundleEntry(FAssetRegistryArchive Ar)
        {
            BundleName = Ar.ReadFName();
            BundleAssets = Ar.ReadArray(() => new FSoftObjectPath(Ar.Header.Version >= FAssetRegistryVersionType.ClassPaths ? new FName(new FTopLevelAssetPath(Ar).ToString()) : Ar.ReadFName(), Ar.ReadFString()));
        }
    }
}

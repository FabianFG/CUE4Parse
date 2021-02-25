using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetPackageData
    {
        public readonly FName PackageName;
        public readonly long DiskSize;
        public readonly FGuid PackageGuid;
        public readonly FMD5Hash? CookedHash;
        
        public FAssetPackageData(FAssetRegistryArchive Ar, bool serializeHash)
        {
            PackageName = Ar.ReadFName();
            DiskSize = Ar.Read<long>();
            PackageGuid = Ar.Read<FGuid>();
            CookedHash = serializeHash ? new FMD5Hash(Ar) : null;
        }
    }
}
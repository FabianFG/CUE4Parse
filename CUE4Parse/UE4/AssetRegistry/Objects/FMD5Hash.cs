using CUE4Parse.UE4.AssetRegistry.Readers;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FMD5Hash
    {
        public readonly byte[]? Hash;
        
        public FMD5Hash(FAssetRegistryArchive Ar)
        {
            Hash = Ar.Read<uint>() != 0 ? Ar.ReadBytes(16) : null;
        }
    }
}
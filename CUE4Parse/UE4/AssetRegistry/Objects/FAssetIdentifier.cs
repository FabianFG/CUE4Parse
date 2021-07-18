using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetIdentifier
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly FName PackageName;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly FName PrimaryAssetType;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly FName ObjectName;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public readonly FName ValueName;

        public FAssetIdentifier(FAssetRegistryArchive Ar)
        {
            var fieldBits = Ar.ReadByte();
            if ((fieldBits & (1 << 0)) != 0)
            {
                PackageName = Ar.ReadFName();
            }
            if ((fieldBits & (1 << 1)) != 0)
            {
                PrimaryAssetType = Ar.ReadFName();
            }
            if ((fieldBits & (1 << 2)) != 0)
            {
                ObjectName = Ar.ReadFName();
            }
            if ((fieldBits & (1 << 3)) != 0)
            {
                ValueName = Ar.ReadFName();
            }
        }
    }
}
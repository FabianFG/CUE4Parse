using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(ObjectPropertyConverter))]
    public class ObjectProperty : FPropertyTagType<FPackageIndex>
    {
        public ObjectProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FPackageIndex(Ar, 0),
                _ => new FPackageIndex(Ar)
            };
        }
    }
}

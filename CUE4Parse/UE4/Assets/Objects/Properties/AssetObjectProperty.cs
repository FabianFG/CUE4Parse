using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(AssetObjectPropertyConverter))]
    public class AssetObjectProperty : FPropertyTagType<string>
    {
        public AssetObjectProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => string.Empty,
                _ => Ar.ReadFString()
            };
        }
    }
}

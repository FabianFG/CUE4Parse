using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(LazyObjectPropertyConverter))]
    public class LazyObjectProperty : FPropertyTagType<FUniqueObjectGuid>
    {
        public LazyObjectProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FUniqueObjectGuid(),
                _ => Ar.Read<FUniqueObjectGuid>()
            };
        }
    }
}

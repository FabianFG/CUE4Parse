using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(NamePropertyConverter))]
    public class NameProperty : FPropertyTagType<FName>
    {
        public NameProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FName(),
                _ => Ar.ReadFName()
            };
        }
    }
}

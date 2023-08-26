using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(FieldPathPropertyConverter))]
    public class FieldPathProperty : FPropertyTagType<FFieldPath>
    {
        public FieldPathProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FFieldPath(),
                _ => new FFieldPath(Ar)
            };
        }
    }
}

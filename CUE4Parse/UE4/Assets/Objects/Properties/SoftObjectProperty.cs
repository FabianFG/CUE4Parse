using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(SoftObjectPropertyConverter))]
public class SoftObjectProperty : FPropertyTagType<FSoftObjectPath>
{
    public SoftObjectProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type == ReadType.ZERO ? new FSoftObjectPath() : new FSoftObjectPath(Ar);
    }
}

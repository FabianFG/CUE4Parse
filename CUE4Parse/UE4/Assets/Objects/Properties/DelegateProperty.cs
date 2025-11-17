using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(DelegatePropertyConverter))]
public class DelegateProperty : FPropertyTagType<FScriptDelegate>
{
    public DelegateProperty(FScriptDelegate value) => Value = value;

    public DelegateProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => new FScriptDelegate(new FPackageIndex(Ar, 0), new FName()),
            _ => new FScriptDelegate(Ar)
        };
    }
}

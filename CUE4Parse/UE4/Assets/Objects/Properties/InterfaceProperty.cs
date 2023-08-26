using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(InterfacePropertyConverter))]
    public class InterfaceProperty : FPropertyTagType<FScriptInterface>
    {
        public InterfaceProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FScriptInterface(),
                _ => new FScriptInterface(Ar)
            };
        }
    }
}

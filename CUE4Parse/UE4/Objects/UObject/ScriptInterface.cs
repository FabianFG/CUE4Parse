using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [JsonConverter(typeof(FScriptInterfaceConverter))]
    public class FScriptInterface
    {
        public FPackageIndex? Object;

        public FScriptInterface(FAssetArchive Ar)
        {
            Object = new FPackageIndex(Ar);
        }

        public FScriptInterface(FPackageIndex? obj = null)
        {
            Object = obj;
        }
    }
}

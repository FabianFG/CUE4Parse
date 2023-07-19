using System;
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

    public class FScriptInterfaceConverter : JsonConverter<FScriptInterface>
    {
        public override void WriteJson(JsonWriter writer, FScriptInterface value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Object);
        }

        public override FScriptInterface ReadJson(JsonReader reader, Type objectType, FScriptInterface existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
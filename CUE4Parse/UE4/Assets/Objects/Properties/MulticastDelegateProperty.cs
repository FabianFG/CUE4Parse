using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(MulticastDelegatePropertyConverter))]
    public class MulticastDelegateProperty : FPropertyTagType<FMulticastScriptDelegate>
    {
        public MulticastDelegateProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FMulticastScriptDelegate(Array.Empty<FScriptDelegate>()),
                _ => new FMulticastScriptDelegate(Ar)
            };
        }
    }

    public class MulticastInlineDelegateProperty : MulticastDelegateProperty
    {
        public MulticastInlineDelegateProperty(FAssetArchive Ar, ReadType type) : base(Ar, type) { }
    }

    public class MulticastSparseDelegateProperty : MulticastDelegateProperty
    {
        public MulticastSparseDelegateProperty(FAssetArchive Ar, ReadType type) : base(Ar, type) { }
    }
}

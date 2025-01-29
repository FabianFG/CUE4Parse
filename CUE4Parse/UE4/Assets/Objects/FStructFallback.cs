using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FStructFallbackConverter))]
    [SkipObjectRegistration]
    public class FStructFallback : AbstractPropertyHolder, IUStruct
    {
        public FStructFallback()
        {
            Properties = [];
        }

        public FStructFallback(FAssetArchive Ar, string? structType) : this(Ar, structType != null ? new UScriptClass(structType) : null) { }

        public FStructFallback(FAssetArchive Ar, UStruct? structType = null)
        {
            if (Ar.HasUnversionedProperties)
            {
                if (structType == null) throw new ArgumentException("For unversioned struct fallback the struct type cannot be null", nameof(structType));
                UObject.DeserializePropertiesUnversioned(Properties = [], Ar, structType);
            }
            else
            {
                UObject.DeserializePropertiesTagged(Properties = [], Ar, true);
            }
        }
    }
}

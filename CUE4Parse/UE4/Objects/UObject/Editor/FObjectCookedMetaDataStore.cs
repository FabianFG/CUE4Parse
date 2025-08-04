using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FObjectCookedMetaDataStore : IUStruct
{
    public Dictionary<FName, string?> ObjectMetaData;

    public FObjectCookedMetaDataStore(FStructFallback fallback)
    {
        ObjectMetaData = new Dictionary<FName, string?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(ObjectMetaData)).Properties)
        {
            ObjectMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<string>();
        }
    }
}

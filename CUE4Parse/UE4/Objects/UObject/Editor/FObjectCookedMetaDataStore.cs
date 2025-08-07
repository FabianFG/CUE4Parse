using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FObjectCookedMetaDataStore : IUStruct
{
    public Dictionary<string, string?> ObjectMetaData;

    public FObjectCookedMetaDataStore(FStructFallback fallback)
    {
        ObjectMetaData = new Dictionary<string, string?>();
        if (!fallback.TryGetValue(out UScriptMap map, nameof(ObjectMetaData)))
            return;

        foreach (var kv in map.Properties)
        {
            ObjectMetaData[kv.Key.GetValue<FName>().PlainText.ToLower()] = kv.Value?.GetValue<string>();
        }
    }
}

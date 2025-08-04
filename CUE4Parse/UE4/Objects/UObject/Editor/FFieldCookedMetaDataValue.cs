using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FFieldCookedMetaDataValue : IUStruct
{
    public Dictionary<FName, string?> MetaData;

    public FFieldCookedMetaDataValue(FStructFallback fallback)
    {
        MetaData = new Dictionary<FName, string?>();
        if (!fallback.TryGetValue(out UScriptMap map, nameof(MetaData)))
            return;

        foreach (var kv in map.Properties)
        {
            MetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<string>();
        }
    }
}

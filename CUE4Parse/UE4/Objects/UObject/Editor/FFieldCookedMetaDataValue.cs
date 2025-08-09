using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FFieldCookedMetaDataValue : IUStruct
{
    public readonly Dictionary<string, string?> MetaData;

    public FFieldCookedMetaDataValue(FStructFallback fallback)
    {
        MetaData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!fallback.TryGetValue(out UScriptMap map, nameof(MetaData)))
            return;

        foreach (var kv in map.Properties)
        {
            MetaData[kv.Key.GetValue<FName>().Text] = kv.Value?.GetValue<string>();
        }
    }
}

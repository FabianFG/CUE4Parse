using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FStructCookedMetaDataStore : IUStruct
{
    public readonly FObjectCookedMetaDataStore ObjectMetaData;
    public readonly Dictionary<FName, FFieldCookedMetaDataStore?> PropertiesMetaData;

    public FStructCookedMetaDataStore(FStructFallback fallback)
    {
        ObjectMetaData = fallback.GetOrDefault<FObjectCookedMetaDataStore>(nameof(ObjectMetaData));
        PropertiesMetaData = new Dictionary<FName, FFieldCookedMetaDataStore?>();

        if (!fallback.TryGetValue(out UScriptMap map, nameof(PropertiesMetaData))) return;
        foreach (var kv in map.Properties)
        {
            PropertiesMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<FFieldCookedMetaDataStore>();
        }
    }
}

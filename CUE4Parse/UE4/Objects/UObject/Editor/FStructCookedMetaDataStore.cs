using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FStructCookedMetaDataStore : IUStruct
{
    public FObjectCookedMetaDataStore ObjectMetaData;
    public Dictionary<FName, FFieldCookedMetaDataStore?> PropertiesMetaData;

    public FStructCookedMetaDataStore(FStructFallback fallback)
    {
        ObjectMetaData = fallback.GetOrDefault<FObjectCookedMetaDataStore>(nameof(ObjectMetaData));
        PropertiesMetaData = new Dictionary<FName, FFieldCookedMetaDataStore?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(PropertiesMetaData)).Properties)
        {
            PropertiesMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<FFieldCookedMetaDataStore>();
        }
    }
}

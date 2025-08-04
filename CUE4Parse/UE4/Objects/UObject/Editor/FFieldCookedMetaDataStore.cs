using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

[StructFallback]
public struct FFieldCookedMetaDataStore : IUStruct
{
    public Dictionary<FName, string?> FieldMetaData;
    public Dictionary<FFieldCookedMetaDataKey, FFieldCookedMetaDataValue?> SubFieldMetaData;

    public FFieldCookedMetaDataStore(FStructFallback fallback)
    {
        FieldMetaData = new Dictionary<FName, string?>();
        if (fallback.TryGetValue(out UScriptMap map1, nameof(FieldMetaData)))
        {
            foreach (var kv in map1.Properties)
            {
                FieldMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<string>();
            }
        }

        SubFieldMetaData = new Dictionary<FFieldCookedMetaDataKey, FFieldCookedMetaDataValue?>();
        if (fallback.TryGetValue(out UScriptMap map2, nameof(SubFieldMetaData)))
        {
            foreach (var kv in map2.Properties)
            {
                SubFieldMetaData[kv.Key.GetValue<FFieldCookedMetaDataKey>()] = kv.Value?.GetValue<FFieldCookedMetaDataValue>();
            }
        }
    }
}

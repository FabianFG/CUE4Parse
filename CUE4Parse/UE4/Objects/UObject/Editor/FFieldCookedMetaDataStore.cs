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
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(FieldMetaData)).Properties)
        {
            FieldMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<string>();
        }

        SubFieldMetaData = new Dictionary<FFieldCookedMetaDataKey, FFieldCookedMetaDataValue?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(SubFieldMetaData)).Properties)
        {
            SubFieldMetaData[kv.Key.GetValue<FFieldCookedMetaDataKey>()] = kv.Value?.GetValue<FFieldCookedMetaDataValue>();
        }
    }
}

using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

public class UClassCookedMetaData : Assets.Exports.UObject
{
    public FStructCookedMetaDataStore ClassMetaData;
    public Dictionary<string, FStructCookedMetaDataStore?> FunctionsMetaData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ClassMetaData = GetOrDefault<FStructCookedMetaDataStore>(nameof(ClassMetaData));
        FunctionsMetaData = new Dictionary<string, FStructCookedMetaDataStore?>(StringComparer.OrdinalIgnoreCase);

        if (!TryGetValue(out UScriptMap map, nameof(FunctionsMetaData))) return;
        foreach (var kv in map.Properties)
        {
            FunctionsMetaData[kv.Key.GetValue<FName>().Text] = kv.Value?.GetValue<FStructCookedMetaDataStore>();
        }
    }
}

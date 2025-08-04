using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject.Editor;

public class UClassCookedMetaData : Assets.Exports.UObject
{
    public FStructCookedMetaDataStore ClassMetaData;
    public Dictionary<FName, FStructCookedMetaDataStore?> FunctionsMetaData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ClassMetaData = GetOrDefault<FStructCookedMetaDataStore>(nameof(ClassMetaData));
        FunctionsMetaData = new Dictionary<FName, FStructCookedMetaDataStore?>();
        foreach (var kv in GetOrDefault<UScriptMap>(nameof(FunctionsMetaData)).Properties)
        {
            FunctionsMetaData[kv.Key.GetValue<FName>()] = kv.Value?.GetValue<FStructCookedMetaDataStore>();
        }
    }
}

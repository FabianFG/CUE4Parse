using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public class UDataTable : UObject
{
    public Dictionary<FName, FStructFallback> RowMap { get; private set; }
    public string? RowStructName { get; protected set; } // Set by inheritor or during deserialization

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        // UObject Properties

        UStruct? rowStruct = null;
        if (string.IsNullOrEmpty(RowStructName))
        {
            var ptr = GetOrDefault<FPackageIndex?>("RowStruct");
            if (ptr != null)
            {
                RowStructName = ptr.Name;
                ptr.TryLoad<UStruct>(out rowStruct);
            }
        }

        var numRows = Ar.Read<int>();
        RowMap = new Dictionary<FName, FStructFallback>(numRows);
        for (var i = 0; i < numRows; i++)
        {
            var rowName = Ar.ReadFName();
            RowMap[rowName] = rowStruct != null ? new FStructFallback(Ar, rowStruct) : new FStructFallback(Ar, RowStructName);
        }

        if (Ar.Game == EGame.GAME_LostSoulAside)
        {
            var DataTableName = Ar.ReadFString();
            var MetaData = Ar.ReadMap(Ar.ReadFString, () => Ar.ReadMap(Ar.ReadFName, Ar.ReadFString));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("Rows");
        serializer.Serialize(writer, RowMap);
    }
}

public static class UDataTableUtility
{
    public static bool TryGetDataTableRow(this UDataTable dataTable, string rowKey, StringComparison comparisonType, out FStructFallback rowValue)
    {
        foreach (var kvp in dataTable.RowMap)
        {
            if (kvp.Key.IsNone || !kvp.Key.Text.Equals(rowKey, comparisonType)) continue;

            rowValue = kvp.Value;
            return true;
        }

        rowValue = default;
        return false;
    }
}

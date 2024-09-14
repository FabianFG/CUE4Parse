using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
    public class UDataTable : UObject
    {
        public Dictionary<FName, FStructFallback> RowMap { get; private set; }
        protected string? RowStructName { get; set; } // Only used if set from inheritor

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties

            UStruct? rowStruct = null;
            if (string.IsNullOrEmpty(RowStructName))
            {
                var ptr = GetOrDefault<FPackageIndex>("RowStruct");
                if (ptr is not null && !ptr.TryLoad<UStruct>(out rowStruct))
                    RowStructName = ptr.Name;
            }

            var numRows = Ar.Read<int>();
            RowMap = new Dictionary<FName, FStructFallback>(numRows);
            for (var i = 0; i < numRows; i++)
            {
                var rowName = Ar.ReadFName();
                RowMap[rowName] = rowStruct != null ? new FStructFallback(Ar, rowStruct) : new FStructFallback(Ar, RowStructName);
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
}

using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
    [JsonConverter(typeof(UDataTableConverter))]
    public class UDataTable : UObject
    {
        public Dictionary<FName, UObject> RowMap { get; private set; }

        public UDataTable() { }
        public UDataTable(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties
            string structType = GetOrDefault<FPackageIndex>("RowStruct").Name; // type of the RowMap values

            var numRows = Ar.Read<int>();
            RowMap = new Dictionary<FName, UObject>(numRows);
            for (var i = 0; i < numRows; i++)
            {
                var rowName = Ar.ReadFName();
                UObject rowValue = new UObject(new List<FPropertyTag>(), null, structType);
                rowValue.Deserialize(Ar, -1);
                RowMap[rowName] = rowValue;
            }
        }
    }

    public static class UDataTableUtility
    {
        public static bool TryGetDataTableRow(this UDataTable dataTable, string rowKey, StringComparison comparisonType, out UObject rowValue)
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
    
    public class UDataTableConverter : JsonConverter<UDataTable>
    {
        public override void WriteJson(JsonWriter writer, UDataTable value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }
            
            // export properties
            writer.WritePropertyName("Export");
            serializer.Serialize(writer, value.RowMap); // will write structType from base Properties
            
            writer.WriteEndObject();
        }

        public override UDataTable ReadJson(JsonReader reader, Type objectType, UDataTable existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

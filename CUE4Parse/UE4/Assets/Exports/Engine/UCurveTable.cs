using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
    public class UCurveTable : UObject
    {
        public Dictionary<FName, FStructFallback> RowMap { get; private set; } // FStructFallback is FRealCurve aka FSimpleCurve if CurveTableMode is SimpleCurves else FRichCurve
        public ECurveTableMode CurveTableMode { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var numRows = Ar.Read<int>();

            var bUpgradingCurveTable = FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ShrinkCurveTableSize;
            if (bUpgradingCurveTable)
                CurveTableMode = numRows > 0 ? ECurveTableMode.RichCurves : ECurveTableMode.Empty;
            else
                CurveTableMode = Ar.Read<ECurveTableMode>();
            RowMap = new Dictionary<FName, FStructFallback>(numRows);
            for (var i = 0; i < numRows; i++)
            {
                var rowName = Ar.ReadFName();
                string rowStruct = CurveTableMode switch
                {
                    ECurveTableMode.SimpleCurves => "SimpleCurve",
                    ECurveTableMode.RichCurves => "RichCurve",
                    _ => ""
                };
                RowMap[rowName] = new FStructFallback(Ar, rowStruct);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Rows");
            serializer.Serialize(writer, RowMap);
        }
    }

    public static class UCurveTableUtility
    {
        public static bool TryGetCurveTableRow(this UCurveTable curveTable, string rowKey, StringComparison comparisonType, out FStructFallback rowValue)
        {
            foreach (var kvp in curveTable.RowMap)
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
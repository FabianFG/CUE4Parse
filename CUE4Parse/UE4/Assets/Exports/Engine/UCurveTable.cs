using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

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

            writer.WritePropertyName("CurveTableMode");
            writer.WriteValue(CurveTableMode.ToString());

            writer.WritePropertyName("Rows");
            serializer.Serialize(writer, RowMap);
        }

        public FRealCurve? FindCurve(FName rowName, bool bWarnIfNotFound = true)
        {
            if (rowName.IsNone)
            {
                if (bWarnIfNotFound) Log.Warning("UCurveTable::FindCurve : NAME_None is invalid row name for CurveTable '{0}'.", GetPathName());
                return null;
            }

            if (!RowMap.TryGetValue(rowName, out var foundCurve))
            {
                if (bWarnIfNotFound) Log.Warning("UCurveTable::FindCurve : Row '{0}' not found in CurveTable '{1}'.", rowName.ToString(), GetPathName());
                return null;
            }

            return CurveTableMode switch
            {
                ECurveTableMode.SimpleCurves => new FSimpleCurve(foundCurve),
                ECurveTableMode.RichCurves => new FRichCurve(foundCurve),
                _ => null
            };
        }

        public bool TryFindCurve(FName rowName, out FRealCurve outCurve, bool bWarnIfNotFound = true)
        {
            if (rowName.IsNone)
            {
                if (bWarnIfNotFound) Log.Warning("UCurveTable::FindCurve : NAME_None is invalid row name for CurveTable '{0}'.", GetPathName());
                outCurve = null;
                return false;
            }

            if (!RowMap.TryGetValue(rowName, out var foundCurve))
            {
                if (bWarnIfNotFound) Log.Warning("UCurveTable::FindCurve : Row '{0}' not found in CurveTable '{1}'.", rowName.ToString(), GetPathName());
                outCurve = null;
                return false;
            }

            switch (CurveTableMode)
            {
                case ECurveTableMode.SimpleCurves:
                    outCurve = new FSimpleCurve(foundCurve);
                    return true;
                case ECurveTableMode.RichCurves:
                    outCurve = new FRichCurve(foundCurve);
                    return true;
            }

            outCurve = null;
            return false;
        }
    }
}

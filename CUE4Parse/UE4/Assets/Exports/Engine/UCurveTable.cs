using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
    public class UCurveTable : UObject
    {
        public Dictionary<FName, UObject> RowMap { get; private set; } // UObject is actually FRealCurve aka FSimpleCurve if CurveTableMode is SimpleCurves else FRichCurve
        public ECurveTableMode CurveTableMode { get; private set; }

        public UCurveTable() { }
        public UCurveTable(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);

            int numRows = Ar.Read<int>();
            CurveTableMode = Ar.Read<ECurveTableMode>();
            RowMap = new Dictionary<FName, UObject>(numRows);
            for(int i = 0; i < numRows; i++)
            {
                FName rowName = Ar.ReadFName();
                string exportType = CurveTableMode switch
                {
                    ECurveTableMode.SimpleCurves => "SimpleCurve",
                    ECurveTableMode.RichCurves => "RichCurve",
                    _ => ""
                };
                UObject rowValue = new UObject(new List<FPropertyTag>(), null, exportType);
                rowValue.Deserialize(Ar);
                RowMap[rowName] = rowValue;
            }
        }
    }
}

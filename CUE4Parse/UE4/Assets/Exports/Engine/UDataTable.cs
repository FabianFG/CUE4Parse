using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Engine
{
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

            int numRows = Ar.Read<int>();
            RowMap = new Dictionary<FName, UObject>(numRows);
            for (int i = 0; i < numRows; i++)
            {
                FName rowName = Ar.ReadFName();
                UObject rowValue = new UObject(new List<FPropertyTag>(), null, structType);
                rowValue.Deserialize(Ar, -1);
                RowMap[rowName] = rowValue;
            }
        }
    }
}

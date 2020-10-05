using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization
{
    public class UStringTable : UObject
    {
        public FStringTable StringTable { get; private set; }
        public int StringTableId { get; private set; } // Index of the string in the NameMap

        public UStringTable() { }
        public UStringTable(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar)
        {
            base.Deserialize(Ar);

            StringTable = new FStringTable(Ar);
            StringTableId = Ar.Read<int>();
        }
    }
}

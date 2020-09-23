using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports
{
    public abstract class UExport
    {
        public string ExportType { get; protected set; }
        public FObjectExport? Export { get; }
        public string Name { get; protected set; }
        public Package? Owner { get; set; }
        
        public abstract void Deserialize(FAssetArchive Ar);

        protected UExport(string exportType)
        {
            ExportType = exportType;
            Name = exportType;
        }

        protected UExport(FObjectExport exportObject) : this(exportObject.ClassIndex.Name)
        {
            Export = exportObject;
            Name = exportObject.ObjectName.Text;
        }

        public override string ToString() => Name;
    }
}
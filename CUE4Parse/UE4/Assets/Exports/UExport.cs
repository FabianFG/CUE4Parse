using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports
{
    public abstract class UExport
    {
        public string ExportType { get; protected set; }
        public FObjectExport? Export { get; }
        public string Name { get; protected set; }
        public IPackage? Owner { get; set; }
        
        public abstract void Deserialize(FAssetArchive Ar, long validPos);

        protected UExport(string exportType)
        {
            ExportType = exportType;
            Name = exportType;
        }

        protected UExport(FObjectExport exportObject) : this(exportObject.ClassName)
        {
            Export = exportObject;
            Name = exportObject.ObjectName.Text;
        }

        public override string ToString() => Name;
    }
}
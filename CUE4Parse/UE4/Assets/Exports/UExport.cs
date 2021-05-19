using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports
{
    // TODO Do we need that abstraction wouldn't UObject be enough?
    public abstract class UExport
    {
        public virtual string ExportType { get; protected set; }
        public FObjectExport? Export { get; }
        public string Name { get; set; }
        public virtual IPackage? Owner { get; set; }
        
        public abstract void Deserialize(FAssetArchive Ar, long validPos);

        // that's actually in UObject
        /** 
	     * Do any object-specific cleanup required immediately after loading an object, 
	     * and immediately after any undo/redo.
	     */
        public abstract void PostLoad();

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

        public override string ToString() => $"{Name} | {ExportType}";
    }
}
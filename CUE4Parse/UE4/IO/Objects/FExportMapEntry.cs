using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FExportMapEntry
    {
        public readonly ulong CookedSerialOffset;
        public readonly ulong CookedSerialSize;
        public readonly FMappedName ObjectName;
        public readonly FPackageObjectIndex OuterIndex;
        public readonly FPackageObjectIndex ClassIndex;
        public readonly FPackageObjectIndex SuperIndex;
        public readonly FPackageObjectIndex TemplateIndex;
        public readonly FPackageObjectIndex GlobalImportIndex;
        public readonly uint ObjectFlags;	// EObjectFlags
        public readonly byte FilterFlags;	// EExportFilterFlags: client/server flags
        private readonly byte _pad0;
        private readonly byte _pad1;
        private readonly byte _pad2;
    }
}
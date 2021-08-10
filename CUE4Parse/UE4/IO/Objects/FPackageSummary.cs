using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPackageSummary
    {
        public readonly FMappedName Name;
        public readonly FMappedName SourceName;
        public readonly EPackageFlags PackageFlags;
        public readonly uint CookedHeaderSize;
        public readonly int NameMapNamesOffset;
        public readonly int NameMapNamesSize;
        public readonly int NameMapHashesOffset;
        public readonly int NameMapHashesSize;
        public readonly int ImportMapOffset;
        public readonly int ExportMapOffset;
        public readonly int ExportBundlesOffset;
        public readonly int GraphDataOffset;
        public readonly int GraphDataSize;
        private readonly int _pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPackageSummary5
    {
        public readonly uint HeaderSize;
        public readonly FMappedName Name;
        // public readonly FMappedName SourceName; // Removed after CL 15151250 of UE5
        public readonly EPackageFlags PackageFlags;
        public readonly uint CookedHeaderSize;
        public readonly int ImportMapOffset;
        public readonly int ExportMapOffset;
        public readonly int ExportBundleEntriesOffset;
        public readonly int GraphDataOffset;
        private readonly int _pad;
    }
}
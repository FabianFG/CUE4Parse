using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FExportBundleHeader
    {
        public readonly uint FirstEntryIndex;
        public readonly uint EntryCount;
    }
}
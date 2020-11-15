using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FScriptObjectEntry
    {
        public readonly FMappedName ObjectName;
        public readonly FPackageObjectIndex GlobalIndex;
        public readonly FPackageObjectIndex OuterIndex;
        public readonly FPackageObjectIndex CDOClassIndex;
    }
}
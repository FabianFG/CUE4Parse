using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FPackageObjectIndex
    {
        public readonly ulong Value;
    }
}
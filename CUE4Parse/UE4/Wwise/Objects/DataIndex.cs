using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DataIndex
    {
        public readonly uint Id;
        public readonly uint Offset;
        public readonly int Length;
    }
}
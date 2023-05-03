using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.VirtualFileCache
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRangeId
    {
        public readonly int FileId;
        public readonly FBlockRange Range;
    }
}

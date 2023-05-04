using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.VirtualFileCache
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FBlockRange
    {
        public readonly int StartIndex;
        public readonly int NumBlocks;

        public override string ToString() => $"Start: {StartIndex} | Blocks: x{NumBlocks}";
    }
}

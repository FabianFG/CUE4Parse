using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine.Level
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPrecomputedVisibilityCell : IUStruct
    {
        public readonly FVector Min;
        public readonly ushort ChunkIndex;
        public readonly ushort DataOffset;
    }
}
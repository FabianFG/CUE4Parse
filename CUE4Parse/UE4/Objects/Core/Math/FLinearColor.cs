using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// A linear, 32-bit/component floating point RGBA color.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FLinearColor : IUStruct
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;
        
        public override string ToString() => $"(R={R},G={G},B={B},A={A})";
    }
}
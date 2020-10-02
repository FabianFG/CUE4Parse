using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FFrameRate : IUStruct
    {
        public readonly int Numerator;
        public readonly int Denominator;
    }
}

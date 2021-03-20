using System.Runtime.InteropServices;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// Stores a color with 8 bits of precision per channel.
    ///
    /// Note: Linear color values should always be converted to gamma space before stored in an FColor, as 8 bits of precision is not enough to store linear space colors!
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FColor : IUStruct
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;
        
        public readonly string Hex => A == 1 || A == 0 ? UnsafePrint.BytesToHex(R, G, B) : UnsafePrint.BytesToHex(A, R, G, B);

        public override string ToString() => Hex;
    }
}
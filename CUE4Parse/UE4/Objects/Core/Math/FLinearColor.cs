using System.Runtime.InteropServices;
using CUE4Parse.Utils;

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

        public readonly string Hex => A == 1 || A == 0
            ? UnsafePrint.BytesToHex((byte) System.Math.Round(R * 255), (byte) System.Math.Round(G * 255), (byte) System.Math.Round(B * 255))
            : UnsafePrint.BytesToHex((byte) System.Math.Round(A * 255), (byte) System.Math.Round(R * 255), (byte) System.Math.Round(G * 255), (byte) System.Math.Round(B * 255));

        public FLinearColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        public override string ToString() => Hex;
    }
}
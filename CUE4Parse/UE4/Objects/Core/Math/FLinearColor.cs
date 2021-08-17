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

        public FColor ToFColor(bool sRGB)
        {
            var floatR = R.Clamp(0.0f, 1.0f);
            var floatG = G.Clamp(0.0f, 1.0f);
            var floatB = B.Clamp(0.0f, 1.0f);
            var floatA = A.Clamp(0.0f, 1.0f);

            if (sRGB)
            {
                floatR = floatR <= 0.0031308f ? floatR * 12.92f : (float)System.Math.Pow(floatR, 1.0f / 2.4f) * 1.055f - 0.055f;
                floatG = floatG <= 0.0031308f ? floatG * 12.92f : (float)System.Math.Pow(floatG, 1.0f / 2.4f) * 1.055f - 0.055f;
                floatB = floatB <= 0.0031308f ? floatB * 12.92f : (float)System.Math.Pow(floatB, 1.0f / 2.4f) * 1.055f - 0.055f;
            }

            var intA = MathUtils.FloorToInt(floatA * 255.999f);
            var intR = MathUtils.FloorToInt(floatR * 255.999f);
            var intG = MathUtils.FloorToInt(floatG * 255.999f);
            var intB = MathUtils.FloorToInt(floatB * 255.999f);

            return new FColor((byte)intR, (byte)intG, (byte)intB, (byte)intA);
        }

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
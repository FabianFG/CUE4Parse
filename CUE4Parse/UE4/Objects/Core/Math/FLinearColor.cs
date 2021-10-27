using System.Runtime.InteropServices;
using CUE4Parse.Utils;
using static System.MathF;

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

        public string Hex => ToFColor(true).Hex;

        public FColor ToFColor(bool sRGB)
        {
            var floatR = R.Clamp(0.0f, 1.0f);
            var floatG = G.Clamp(0.0f, 1.0f);
            var floatB = B.Clamp(0.0f, 1.0f);
            var floatA = A.Clamp(0.0f, 1.0f);

            if (sRGB)
            {
                floatR = floatR <= 0.0031308f ? floatR * 12.92f : Pow(floatR, 1.0f / 2.4f) * 1.055f - 0.055f;
                floatG = floatG <= 0.0031308f ? floatG * 12.92f : Pow(floatG, 1.0f / 2.4f) * 1.055f - 0.055f;
                floatB = floatB <= 0.0031308f ? floatB * 12.92f : Pow(floatB, 1.0f / 2.4f) * 1.055f - 0.055f;
            }

            var intA = (floatA * 255.999f).FloorToInt();
            var intR = (floatR * 255.999f).FloorToInt();
            var intG = (floatG * 255.999f).FloorToInt();
            var intB = (floatB * 255.999f).FloorToInt();

            return new FColor((byte) intR, (byte) intG, (byte) intB, (byte) intA);
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
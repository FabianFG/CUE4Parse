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
        public float R;
        public float G;
        public float B;
        public float A;

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

        public FLinearColor ToSRGB()
        {
            var floatR = R.Clamp(0.0f, 1.0f);
            var floatG = G.Clamp(0.0f, 1.0f);
            var floatB = B.Clamp(0.0f, 1.0f);

            floatR = floatR <= 0.0031308f ? floatR * 12.92f : Pow(floatR, 1.0f / 2.4f) * 1.055f - 0.055f;
            floatG = floatG <= 0.0031308f ? floatG * 12.92f : Pow(floatG, 1.0f / 2.4f) * 1.055f - 0.055f;
            floatB = floatB <= 0.0031308f ? floatB * 12.92f : Pow(floatB, 1.0f / 2.4f) * 1.055f - 0.055f;

            return new FLinearColor(floatR, floatG, floatB, A);
        }

        public FLinearColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public override string ToString() => Hex;

        public FLinearColor LinearRGBToHsv()
        {
            var rgbMin = UnrealMath.Min3(R, G, B);
            var rgbMax = UnrealMath.Max3(R, G, B);
            var rgbRange = rgbMax - rgbMin;

            var hue = rgbMax == rgbMin ? 0.0f :
                rgbMax == R ? UnrealMath.Fmod((G - B) / rgbRange * 60.0f + 360.0f, 360.0f) :
                rgbMax == G ? (B - R) / rgbRange * 60.0f + 120.0f :
                rgbMax == B ? (R - G) / rgbRange * 60.0f + 240.0f :
                0.0f;

            var saturation = rgbMax == 0.0f ? 0.0f : rgbRange / rgbMax;
            return new FLinearColor(hue, saturation, rgbMax, A);
        }

        public FLinearColor HSVToLinearRGB()
        {
            var hue = R;
            var saturation = G;
            var value = B;
            var hDiv60 = hue / 60.0f;
            var hDiv60Floor = Floor(hDiv60);
            var hDiv60Fraction = hDiv60 - hDiv60Floor;

            var rgbValues = new[]
            {
                value,
                value * (1.0f - saturation),
                value * (1.0f - hDiv60Fraction * saturation),
                value * (1.0f - (1.0f - hDiv60Fraction) * saturation)
            };
            var rgbSwizzle = new[]
            {
                new uint[] { 0, 3, 1 },
                new uint[] { 2, 0, 1 },
                new uint[] { 1, 0, 3 },
                new uint[] { 1, 2, 0 },
                new uint[] { 3, 1, 0 },
                new uint[] { 0, 1, 2 }
            };

            var swizzleIndex = (uint) hDiv60Floor % 6;

            return new FLinearColor(rgbValues[rgbSwizzle[swizzleIndex][0]],
                rgbValues[rgbSwizzle[swizzleIndex][1]],
                rgbValues[rgbSwizzle[swizzleIndex][2]],
                A);
        }
    }
}

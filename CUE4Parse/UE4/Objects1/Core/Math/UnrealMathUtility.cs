using System;
using System.Runtime.CompilerServices;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public static class UnrealMath
    {
        public const float SmallNumber = 1e-8f;
        public const float KindaSmallNumber = 1e-4f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min3(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max3(float a, float b, float c) => MathF.Max(a, MathF.Max(b, c));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNearlyEqual(float a, float b, float err = SmallNumber) => MathF.Abs(a - b) <= err;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNearlyZero(float x, float tolerance = KindaSmallNumber) => MathF.Abs(x) <= tolerance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNearlyZero(double x, double tolerance = KindaSmallNumber) => System.Math.Abs(x) <= tolerance;

        public static float Fmod(float x, float y)
        {
            var absY = MathF.Abs(y);
            if (absY <= SmallNumber) return 0;
            var div = x / y;
            var quotient = MathF.Abs(div) < 8388608 ? div.TruncToInt() : div;
            var intPortion = y * quotient;

            if (MathF.Abs(intPortion) > MathF.Abs(x))
            {
                intPortion = x;
            }

            var res = x - intPortion;
            return res.Clamp(-absY, absY);
        }
    }
}

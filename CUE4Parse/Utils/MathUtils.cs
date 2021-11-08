using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.Utils
{
    public static class MathUtils
    {
        public static bool IsNumericType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static float InvSqrt(this float x)
        {
            float xhalf = 0.5f * x;
            int i = BitConverter.SingleToInt32Bits(x);
            i = 0x5f3759df - (i >> 1);
            x = BitConverter.Int32BitsToSingle(i);
            x = x * (1.5f - xhalf * x * x);
            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DivideAndRoundUp(this int dividend, int divisor) => (dividend + divisor - 1) / divisor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(this float radVal) => radVal * (180.0f / MathF.PI);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(this float degVal) => degVal * (MathF.PI / 180.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Square(this float val) => val * val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TruncToInt(this float f) => (int) f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TruncToInt(this double f) => (int) f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToInt(this float f) => Math.Floor(f).TruncToInt();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(this float f) => FloorToInt(f + 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(this int i, int min, int max) => i < min ? min : i < max ? i : max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(this float f, float min, float max) => f < min ? min : f < max ? f : max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FloatSelect(float comparand, float valueGEZero, float valueLTZero) =>
            comparand >= 0.0f ? valueGEZero : valueLTZero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector Lerp(FVector a, FVector b, float alpha) => a + alpha * (b - a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float alpha) => a + alpha * (b - a);

        // FVector -> System.Numerics.Vector

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this FVector2D v) => new(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this FVector v) => new(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(this FVector v) => new(v.X, v.Y, v.Z, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(this FVector4 v) => new(v.X, v.Y, v.Z, v.W);

        // System.Numerics.Vector -> FVector

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector2D ToFVector2D(this Vector2 v) => new(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector ToFVector(this Vector3 v) => new(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector4 ToFVector4(this Vector3 v) => new(v.X, v.Y, v.Z, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector4 ToFVector4(this Vector4 v) => new(v.X, v.Y, v.Z, v.W);

        // FQuat -> System.Numerics.Quaternion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToQuaternion(this FQuat q) => new(q.X, q.Y, q.Z, q.W);

        // System.Numerics.Quaternion -> FQuat

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FQuat ToFQuat(this Quaternion q) => new(q.X, q.Y, q.Z, q.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Ulpc(double value) => BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) + 1) - value;
    }

    public class CubicCurve2D
    {
        public static int SolveCubic(ref double[] coeff, ref double[] solution)
        {
            int numSolutions;
            var a = coeff[2] / coeff[3];
            var b = coeff[1] / coeff[3];
            var c = coeff[0] / coeff[3];

            var sqOfA = a * a;
            var p = 1.0 / 3 * (-1.0 / 3 * sqOfA + b);
            var q = 1.0 / 2 * (2.0 / 27 * a * sqOfA - 1.0 / 3 * a * b + c);

            var cubeOfP = p * p * p;
            var d = q * q + cubeOfP;

            if (UnrealMath.IsNearlyZero(d))
            {
                if (UnrealMath.IsNearlyZero(q)) // one triple solution
                {
                    solution[0] = 0;
                    numSolutions = 1;
                }
                else // one single and one double solution
                {
                    var u = Cbrt(-q);
                    solution[0] = 2 * u;
                    solution[1] = -u;
                    numSolutions = 2;
                }
            }
            else if (d < 0) // Casus irreducibilis: three real solutions
            {
                var phi = 1.0 / 3 * Math.Acos(-q / Math.Sqrt(-cubeOfP));
                var t = 2 * Math.Sqrt(-p);

                solution[0] = t * Math.Cos(phi);
                solution[1] = -t * Math.Cos(phi + Math.PI / 3);
                solution[2] = -t * Math.Cos(phi - Math.PI / 3);
                numSolutions = 3;
            }
            else // one real solution
            {
                var sqrtD = Math.Sqrt(d);
                var u = Cbrt(sqrtD - q);
                var v = -Cbrt(sqrtD + q);

                solution[0] = u + v;
                numSolutions = 1;
            }

            var sub = 1.0 / 3 * a;

            for (var i = 0; i < numSolutions; ++i)
            {
                solution[i] -= sub;
            }

            return numSolutions;
        }

        private static double Cbrt(double x) => x > 0.0 ? Math.Pow(x, 1.0 / 3.0) : x < 0.0 ? -Math.Pow(-x, 1.0 / 3.0) : 0.0;
    }
}

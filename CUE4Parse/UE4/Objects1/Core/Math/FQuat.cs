using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using static System.MathF;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public enum EForceInit
    {
        ForceInit,
        ForceInitToZero
    }

    /// <summary>
    /// USE Ar.Read<FQuat> FOR FLOATS AND new FQuat(Ar) FOR DOUBLES
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FQuat : IUStruct
    {
        public const float THRESH_QUAT_NORMALIZED = 0.01f;   /** Allowed error for a normalized quaternion (against squared magnitude) */

        public static readonly FQuat Identity = new(0, 0, 0, 1);

        public float X;
        public float Y;
        public float Z;
        public float W;

        public FQuat(EForceInit zeroOrNot)
        {
            X = 0;
            Y = 0;
            Z = 0;
            W = zeroOrNot == EForceInit.ForceInitToZero ? 0 : 1;
        }

        public FQuat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public FQuat(TIntVector4<float> quat)
        {
            X = quat.X;
            Y = quat.Y;
            Z = quat.Z;
            W = quat.W;
        }

        public FQuat(TIntVector4<double> quat)
        {
            X = (float) quat.X;
            Y = (float) quat.Y;
            Z = (float) quat.Z;
            W = (float) quat.W;
        }

        public FQuat(FArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                X = (float) Ar.Read<double>();
                Y = (float) Ar.Read<double>();
                Z = (float) Ar.Read<double>();
                W = (float) Ar.Read<double>();
            }
            else
            {
                X = Ar.Read<float>();
                Y = Ar.Read<float>();
                Z = Ar.Read<float>();
                W = Ar.Read<float>();
            }
        }

        private static int[] matrixNxt = {1, 2, 0};
        public FQuat(FMatrix m)
        {
            // If Matrix is NULL, return Identity quaternion. If any of them is 0, you won't be able to construct rotation
            // if you have two plane at least, we can reconstruct the frame using cross product, but that's a bit expensive op to do here
            // for now, if you convert to matrix from 0 scale and convert back, you'll lose rotation. Don't do that.
            if (m.GetScaledAxis(EAxis.X).IsNearlyZero() || m.GetScaledAxis(EAxis.Y).IsNearlyZero() || m.GetScaledAxis(EAxis.Z).IsNearlyZero())
            {
                var id = Identity;
                X = id.X;
                Y = id.Y;
                Z = id.Z;
                W = id.W;
                return;
            }

            //const MeReal *const t = (MeReal *) tm;
            float s;

            // Check diagonal (trace)
            var tr = m.M00 + m.M11 + m.M22;

            if (tr > 0.0f)
            {
                var invS = 1.0f / Sqrt(tr + 1.0f);
                W = 0.5f * (1.0f / invS);
                s = 0.5f * invS;

                X = (m.M12 - m.M21) * s;
                Y = (m.M20 - m.M02) * s;
                Z = (m.M01 - m.M10) * s;
            }
            else
            {
                // diagonal is negative
                var i = 0;

                if (m.M11 > m.M00)
                    i = 1;

                if (m.M22 > m[4*i+i])
                    i = 2;

                var j = matrixNxt[i];
                var k = matrixNxt[j];

                s = m[4*i+i] - m[4*j+j] - m[4*k+k] + 1.0f;

                var invS = 1.0f / Sqrt(s);

                Span<float> qt = stackalloc float[4];
                qt[i] = 0.5f * (1.0f / invS);

                s = 0.5f * invS;

                qt[3] = (m[4*j+k] - m[4*k+j]) * s;
                qt[j] = (m[4*i+j] + m[4*j+i]) * s;
                qt[k] = (m[4*i+k] + m[4*k+i]) * s;

                X = qt[0];
                Y = qt[1];
                Z = qt[2];
                W = qt[3];
            }
        }

        public FQuat(FRotator rotator)
        {
            var quat = rotator.Quaternion();
            X = quat.X;
            Y = quat.Y;
            Z = quat.Z;
            W = quat.W;
        }

        public FQuat(FVector axis, float angleRad)
        {
            var halfA = 0.5f * angleRad;
            var s = Sin(halfA);
            var c = Cos(halfA);

            X = s * axis.X;
            Y = s * axis.Y;
            Z = s * axis.Z;
            W = c;
        }

        public bool Equals(FQuat q, float tolerance) => (Abs(X - q.X) <= tolerance && Abs(Y - q.Y) <= tolerance && Abs(Z - q.Z) <= tolerance && Abs(W - q.W) <= tolerance) ||
                                                        (Abs(X + q.X) <= tolerance && Abs(Y + q.Y) <= tolerance && Abs(Z + q.Z) <= tolerance && Abs(W + q.W) <= tolerance);

        public bool IsIdentity(float tolerance = UnrealMath.SmallNumber) => Equals(Identity, tolerance);

        public static Vector128<float> AsVector128(FQuat value)
        {
            return Unsafe.As<FQuat, Vector128<float>>(ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FQuat operator *(FQuat a, FQuat b)
        {
            // both yield different results idk why
            if (Sse.IsSupported)
            {
                return UnrealMathSSE.VectorQuaternionMultiply2(a, b);
            }

            var r = new FQuat();
            var t0 = (a.Z - a.Y) * (b.Y - b.Z);
            var t1 = (a.W + a.X) * (b.W + b.X);
            var t2 = (a.W - a.X) * (b.Y + b.Z);
            var t3 = (a.Y + a.Z) * (b.W - b.X);
            var t4 = (a.Z - a.X) * (b.X - b.Y);
            var t5 = (a.Z + a.X) * (b.X + b.Y);
            var t6 = (a.W + a.Y) * (b.W - b.Z);
            var t7 = (a.W - a.Y) * (b.W + b.Z);
            var t8 = t5 + t6 + t7;
            var t9 = 0.5f * (t4 + t8);

            r.X = t1 + t9 - t8;
            r.Y = t2 + t9 - t7;
            r.Z = t3 + t9 - t6;
            r.W = t0 + t9 - t5;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator *(FQuat a, FVector b) => a.RotateVector(b);

        public static bool operator ==(FQuat a, FQuat b) =>
            a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;

        public static bool operator !=(FQuat a, FQuat b) => !(a == b);

        public void Normalize(float tolerance = UnrealMath.SmallNumber)
        {
            var squareSum = X * X + Y * Y + Z * Z + W * W;

            if (squareSum >= tolerance)
            {
                var scale = squareSum.InvSqrt();

                X *= scale;
                Y *= scale;
                Z *= scale;
                W *= scale;
            }
            else
            {
                var id = Identity;
                X = id.X;
                Y = id.Y;
                Z = id.Z;
                W = id.W;
            }
        }

        public FQuat GetNormalized(float tolerance = UnrealMath.SmallNumber)
        {
            var result = this;
            result.Normalize(tolerance);
            return result;
        }

        public bool IsNormalized => Abs(1f - SizeSquared) < THRESH_QUAT_NORMALIZED;

        public float Size => Sqrt(SizeSquared);

        public float SizeSquared => X * X + Y * Y + Z * Z + W * W;

        public FVector RotateVector(FVector v)
        {
            // http://people.csail.mit.edu/bkph/articles/Quaternions.pdf
            // V' = V + 2w(Q x V) + (2Q x (Q x V))
            // refactor:
            // V' = V + w(2(Q x V)) + (Q x (2(Q x V)))
            // T = 2(Q x V);
            // V' = V + w*(T) + (Q x T)

            var q = new FVector(X, Y, Z);
            var t = 2.0f * FVector.CrossProduct(q, v);
            return v + (W * t) + FVector.CrossProduct(q, t);
        }

        public FVector UnrotateVector(FVector v)
        {
            var q = new FVector(-X, -Y, -Z); // Inverse
            var t = 2.0f * FVector.CrossProduct(q, v);
            return v + (W * t) + FVector.CrossProduct(q, t);
        }

        public FQuat Inverse() => IsNormalized
            ? new FQuat(-X, -Y, -Z, W)
            : throw new ArgumentException("Quat must be normalized in order to be inversed");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Conjugate() // public FQuat Inverse()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        public static FQuat Conjugate(FQuat quat)
        {
            return new FQuat(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        public static FQuat FindBetweenNormals(FVector a, FVector b, float normAb = 1.0f)
        {
            float w = normAb + FVector.DotProduct(a, b);
            FQuat result;

            if (w >= 1e-6f * normAb)
            {
                //Axis = FVector::CrossProduct(A, B);
                result = new FQuat(
                    a.Y * b.Z - a.Z * b.Y,
                    a.Z * b.X - a.X * b.Z,
                    a.X * b.Y - a.Y * b.X,
                    w);
            }
            else
            {
                // A and B point in opposite directions
                w = 0.0f;
                result = Abs(a.X) > Abs(a.Y)
                    ? new FQuat(-a.Z, 0.0f, a.X, w)
                    : new FQuat(0.0f, -a.Z, a.Y, w);
            }

            result.Normalize();
            return result;
        }

        public FRotator Rotator()
        {
            var singularityTest = Z * X - W * Y;
            var yawY = 2.0f * (W * Z + X * Y);
            var yawX = (1.0f - 2.0f * (Y * Y + Z * Z));

            // reference
            // http://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
            // http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            // this value was found from experience, the above websites recommend different values
            // but that isn't the case for us, so I went through different testing, and finally found the case
            // where both of world lives happily.
            const float SINGULARITY_THRESHOLD = 0.4999995f;
            const float RAD_TO_DEG = 180.0f / PI;
            var rotatorFromQuat = new FRotator();

            if (singularityTest < -SINGULARITY_THRESHOLD)
            {
                rotatorFromQuat.Pitch = -90.0f;
                rotatorFromQuat.Yaw = Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = FRotator.NormalizeAxis(-rotatorFromQuat.Yaw - (2.0f * Atan2(X, W) * RAD_TO_DEG));
            }
            else if (singularityTest > SINGULARITY_THRESHOLD)
            {
                rotatorFromQuat.Pitch = 90.0f;
                rotatorFromQuat.Yaw = Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = FRotator.NormalizeAxis(rotatorFromQuat.Yaw - (2.0f * Atan2(X, W) * RAD_TO_DEG));
            }
            else
            {
                rotatorFromQuat.Pitch = Asin(2.0f * singularityTest) * RAD_TO_DEG;
                rotatorFromQuat.Yaw = Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = Atan2(-2.0f * (W * X + Y * Z), (1.0f - 2.0f * (X * X + Y * Y))) * RAD_TO_DEG;
            }

            return rotatorFromQuat;
        }

        public bool ContainsNaN() =>
            !float.IsFinite(X) ||
            !float.IsFinite(Y) ||
            !float.IsFinite(Z) ||
            !float.IsFinite(W);

        public override string ToString() => $"X={X:F9} Y={Y:F9} Z={Z:F9} W={W:F9}";

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(X);
            Ar.Write(Y);
            Ar.Write(Z);
            Ar.Write(W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FQuat FastLerp(FQuat q1, FQuat q2, float alpha)
        {
            float doResult = q1 | q2;
            float bias = MathUtils.FloatSelect(doResult, 1.0f, -1.0f);
            return (q2 * alpha) + (q1 * (bias * (1f - alpha)));
        }

        public static FQuat Slerp_NotNormalized(FQuat quat1, FQuat quat2, float Slerp)
        {
            // Get cosine of angle between quats.
            var rawCosom =
                quat1.X * quat2.X +
                quat1.Y * quat2.Y +
                quat1.Z * quat2.Z +
                quat1.W * quat2.W;
            // Unaligned quats - compensate, results in taking shorter route.
            var cosom = MathUtils.FloatSelect(rawCosom, rawCosom, -rawCosom);

            float scale0, scale1;

            if (cosom < 0.9999f)
            {
                var omega = Acos(cosom);
                var invSin = 1.0f / Sin(omega);
                scale0 = Sin((1.0f - Slerp) * omega) * invSin;
                scale1 = Sin(Slerp * omega) * invSin;
            }
            else
            {
                // Use linear interpolation.
                scale0 = 1.0f - Slerp;
                scale1 = Slerp;
            }

            // In keeping with our flipped cosom:
            scale1 = MathUtils.FloatSelect(rawCosom, scale1, -scale1);

            return new FQuat
            {
                X = scale0 * quat1.X + scale1 * quat2.X,
                Y = scale0 * quat1.Y + scale1 * quat2.Y,
                Z = scale0 * quat1.Z + scale1 * quat2.Z,
                W = scale0 * quat1.W + scale1 * quat2.W
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FQuat Slerp(FQuat quat1, FQuat quat2, float slerp) => Slerp_NotNormalized(quat1, quat2, slerp).GetNormalized();

        public static float operator |(FQuat a, FQuat b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        public static FQuat operator *(FQuat a, float scale) => new FQuat(scale * a.X, scale * a.Y, scale * a.Z, scale * a.W);
        public static FQuat operator +(FQuat a, FQuat b) => new FQuat(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

        public static implicit operator Quaternion(FQuat v) => new(v.X, v.Y, v.Z, v.W);
    }
}

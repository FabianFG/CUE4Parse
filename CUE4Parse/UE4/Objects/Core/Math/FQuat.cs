using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public enum EForceInit
    {
        ForceInit,
        ForceInitToZero
    }
    
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

        public FQuat(FRotator rotator)
        {
            this = rotator.Quaternion();
        }

        private static int[] matrixNxt = {1, 2, 0};
        public FQuat(FMatrix m)
        {
            // If Matrix is NULL, return Identity quaternion. If any of them is 0, you won't be able to construct rotation
            // if you have two plane at least, we can reconstruct the frame using cross product, but that's a bit expensive op to do here
            // for now, if you convert to matrix from 0 scale and convert back, you'll lose rotation. Don't do that. 
            if (m.GetScaledAxis(EAxis.X).IsNearlyZero() || m.GetScaledAxis(EAxis.Y).IsNearlyZero() || m.GetScaledAxis(EAxis.Z).IsNearlyZero())
            {
                this = Identity;
                return;
            }
            
            //const MeReal *const t = (MeReal *) tm;
            float s;
            
            // Check diagonal (trace)
            var tr = m.M00 + m.M11 + m.M22;

            if (tr > 0.0f)
            {
                var invS = (tr + 1).InvSqrt();
                this.W = 0.5f * (1f / invS);
                s = 0.5f * invS;

                this.X = (m.M12 - m.M21) * s;
                this.Y = (m.M20 - m.M02) * s;
                this.Z = (m.M01 - m.M10) * s;
            }
            else
            {
                // diagonal is negative
                var i = 0;

                if (m.M11 > m.M00)
                    i = 1;

                if (m.M22 > (i == 1 ? m.M11 : m.M00))
                    i = 2;

                var j = matrixNxt[i];
                var k = matrixNxt[j];

                s = (i switch { 0 => m.M00, 1 => m.M11, _ => m.M22}) - (j switch { 0 => m.M00, 1 => m.M11, _ => m.M22}) + 1.0f;

                var invS = s.InvSqrt();

                Span<float> qt = stackalloc float[4];
                qt[i] = 0.5f * (1f / invS);

                s = 0.5f * invS;

                qt[3] = (j switch {0 => k switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => k switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => k switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) - (k switch {0 => j switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => j switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => j switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) * s;
                qt[j] = (i switch {0 => j switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => j switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => j switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) - (j switch {0 => i switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => i switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => i switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) * s;
                qt[k] = (i switch {0 => k switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => k switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => k switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) - (k switch {0 => i switch {0 => m.M00, 1 => m.M01, _ => m.M02}, 1 => i switch {0 => m.M10, 1 => m.M11, _ => m.M12}, _ => i switch {0 => m.M10, 1 => m.M11, _ => m.M12}}) * s;

                this.X = qt[0];
                this.Y = qt[1];
                this.Z = qt[2];
                this.W = qt[3];
            }
        }

        public float SizeSquared => X * X + Y * Y + Z * Z + W * W;
        public float Size => (float) System.Math.Sqrt(SizeSquared);

        public bool IsNormalized => System.Math.Abs(1f - SizeSquared) < THRESH_QUAT_NORMALIZED;

        public FQuat Inverse() => IsNormalized
            ? new FQuat(-X, -Y, -Z, W)
            : throw new ArgumentException("Quat must be normalized to be inversed");

        public void Normalize(float tolerance = FVector.SmallNumber)
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
                this = Identity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Conjugate() // public FQuat Inverse()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Negate()
        {
            X = -X;
            Y = -Y;
            W = -W;
        }

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(X);
            Ar.Write(Y);
            Ar.Write(Z);
            Ar.Write(W);
        }

        public FQuat GetNormalized(float tolerance = FVector.SmallNumber)
        {
            var result = this;
            result.Normalize(tolerance);
            return result;
        }

        public bool ContainsNaN()
        {
            return !float.IsFinite(X) ||
                   !float.IsFinite(Y) ||
                   !float.IsFinite(Z) ||
                   !float.IsFinite(W);
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
            const float RAD_TO_DEG = 180.0f / (float) System.Math.PI;
            var rotatorFromQuat = new FRotator();

            if (singularityTest < -SINGULARITY_THRESHOLD)
            {
                rotatorFromQuat.Pitch = -90.0f;
                rotatorFromQuat.Yaw = (float) System.Math.Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = FRotator.NormalizeAxis(-rotatorFromQuat.Yaw - (2.0f * (float) System.Math.Atan2(X, W) * RAD_TO_DEG));
            }
            else if (singularityTest > SINGULARITY_THRESHOLD)
            {
                rotatorFromQuat.Pitch = 90.0f;
                rotatorFromQuat.Yaw = (float) System.Math.Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = FRotator.NormalizeAxis(rotatorFromQuat.Yaw - (2.0f * (float) System.Math.Atan2(X, W) * RAD_TO_DEG));
            }
            else
            {
                rotatorFromQuat.Pitch = (float) System.Math.Asin(2.0f * singularityTest) * RAD_TO_DEG;
                rotatorFromQuat.Yaw = (float) System.Math.Atan2(yawY, yawX) * RAD_TO_DEG;
                rotatorFromQuat.Roll = (float) (System.Math.Atan2(-2.0f * (W * X + Y * Z), (1.0f - 2.0f * (X * X + Y * Y))) * RAD_TO_DEG);
            }

            return rotatorFromQuat;
        }
        
        public static bool operator ==(FQuat a, FQuat b) =>
            a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;

        public static bool operator !=(FQuat a, FQuat b) => !(a == b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FQuat operator *(FQuat a, FQuat b)
        {
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

        public FVector RotateVector(FVector v)
        {
            // http://people.csail.mit.edu/bkph/articles/Quaternions.pdf
            // V' = V + 2w(Q x V) + (2Q x (Q x V))
            // refactor:
            // V' = V + w(2(Q x V)) + (Q x (2(Q x V)))
            // T = 2(Q x V);
            // V' = V + w*(T) + (Q x T)
            
            var q = new FVector(X, Y, Z);
            var t = FVector.CrossProduct(q, v) * 2.0f;
            return v + (t * W) + FVector.CrossProduct(q, t);
        }

        public static FVector operator *(FQuat a, FVector b) => a.RotateVector(b);
        
        public bool Equals(FQuat q, float tolerance) => (System.Math.Abs(X - q.X) <= tolerance && System.Math.Abs(Y - q.Y) <= tolerance && System.Math.Abs(Z - q.Z) <= tolerance && System.Math.Abs(W - q.W) <= tolerance) ||
                                                        (System.Math.Abs(X + q.X) <= tolerance && System.Math.Abs(Y + q.Y) <= tolerance && System.Math.Abs(Z + q.Z) <= tolerance && System.Math.Abs(W + q.W) <= tolerance);

        public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}, {nameof(W)}: {W}";
    }
}

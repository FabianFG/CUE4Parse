using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FRotator : IUStruct
    {
        private const float KindaSmallNumber = 1e-4f;
        
        public static readonly FRotator ZeroRotator = new(0, 0, 0);

        public float Pitch;
        public float Yaw;
        public float Roll;

        public FRotator(float f) : this(f, f, f) { }
        public FRotator(float pitch, float yaw, float roll)
        {
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
        }

        public FVector RotateVector(FVector v)
        {
            return new(new FRotationMatrix(this).TransformFVector(v));
        }

        public FVector UnrotateVector(FVector v)
        {
            return new(new FRotationMatrix(this).GetTransposed().TransformFVector(v));
        }

        public FQuat Quaternion()
        {
            //PLATFORM_ENABLE_VECTORINTRINSICS
            const float DEG_TO_RAD = (float) (System.Math.PI / 180.0f);
            const float DIVIDE_BY_2 = DEG_TO_RAD / 2.0f;
            float sp, sy, sr;
            float cp, cy, cr;

            sp = (float) System.Math.Sin(Pitch * DIVIDE_BY_2);
            cp = (float) System.Math.Cos(Pitch * DIVIDE_BY_2);
            sy = (float) System.Math.Sin(Yaw * DIVIDE_BY_2);
            cy = (float) System.Math.Cos(Yaw * DIVIDE_BY_2);
            sr = (float) System.Math.Sin(Roll * DIVIDE_BY_2);
            cr = (float) System.Math.Cos(Roll * DIVIDE_BY_2);

            var rotationQuat = new FQuat
            {
                X = cr * sp * sy - sr * cp * cy,
                Y = -cr * sp * cy - sr * cp * sy,
                Z = cr * cp * sy - sr * sp * cy,
                W = cr * cp * cy + sr * sp * sy
            };

            return rotationQuat;
        }

        public void Normalize()
        {
            Pitch = NormalizeAxis(Pitch);
            Yaw = NormalizeAxis(Yaw);
            Roll = NormalizeAxis(Roll);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FRotator GetNormalized()
        {
            var rot = this;
            rot.Normalize();
            return rot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClampAxis(float angle)
        {
            // returns Angle in the range (-360,360)
            angle %= 360.0f;

            if (angle < 0.0f)
            {
                // shift to [0,360) range
                angle += 360.0f;
            }

            return angle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizeAxis(float angle)
        {
            // returns Angle in the range [0,360)
            angle = ClampAxis(angle);

            if (angle > 180.0f)
            {
                // shift to (-180,180]
                angle -= 360.0f;
            }

            return angle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FRotator a, FRotator b) =>
            a.Pitch == b.Pitch && a.Yaw == b.Yaw && a.Roll == b.Roll;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FRotator a, FRotator b) => !(a == b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FRotator r, float tolerance = KindaSmallNumber) => System.Math.Abs(NormalizeAxis(Pitch - r.Pitch)) <= tolerance &&
                                                           System.Math.Abs(NormalizeAxis(Yaw - r.Yaw)) <= tolerance &&
                                                           System.Math.Abs(NormalizeAxis(Roll - r.Roll)) <= tolerance;

        public override string ToString() => $"P={Pitch} Y={Yaw} R={Roll}";
    }
}

using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRotator : IUStruct
    {
        private const float KindaSmallNumber = 1e-4f;

        public readonly float Pitch;
        public readonly float Yaw;
        public readonly float Roll;

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

        public override string ToString() => $"P={Pitch} Y={Yaw} R={Roll}";
    }
}

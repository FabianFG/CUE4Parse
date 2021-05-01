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

        public override string ToString() => $"P={Pitch} Y={Yaw} R={Roll}";
    }
}

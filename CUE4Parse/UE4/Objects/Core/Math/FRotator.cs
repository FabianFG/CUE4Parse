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

        /* Fabian wtf is this hell :'( */

        public override string ToString() => $"P={Pitch} Y={Yaw} R={Roll}";
    }
}

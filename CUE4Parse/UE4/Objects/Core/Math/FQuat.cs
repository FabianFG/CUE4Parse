using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FQuat : IUStruct
    {

        public static readonly FQuat Identity = new(0, 0, 0, 1);
        
        public float X;
        public float Y;
        public float Z;
        public float W;

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

        public bool ContainsNaN()
        {
            return !float.IsFinite(X) ||
                   !float.IsFinite(Y) ||
                   !float.IsFinite(Z) ||
                   !float.IsFinite(W);
        }

        public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}, {nameof(W)}: {W}";
    }
}

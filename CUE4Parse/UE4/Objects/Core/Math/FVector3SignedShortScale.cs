using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FVector3SignedShortScale : IUStruct
    {
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly short W;

        public FVector3SignedShortScale(short x, short y, short z, short w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static implicit operator FVector(FVector3SignedShortScale v)
        {
            // W having the value of short.MaxValue makes me believe I should use it (somehow) instead of a hardcoded constant
            float wf = v.W == 0 ? 1f : v.W;
            return new(v.X / wf, v.Y / wf, v.Z / wf);
        }
    }
}

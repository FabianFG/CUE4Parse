using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.MathF;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FPlane : IUStruct
    {
        public FVector Vector;

        public float X
        {
            get => Vector.X;
            set => Vector.X = value;
        }

        public float Y
        {
            get => Vector.Y;
            set => Vector.Y = value;
        }

        public float Z
        {
            get => Vector.Z;
            set => Vector.Z = value;
        }

        /** The w-component. */
        public float W;

        public FPlane(FVector @base, FVector normal)
        {
            Vector = @base;
            W = @base | normal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float PlaneDot(FVector p) => X * p.X + Y * p.Y + Z * p.Z - W;

        public bool Equals(FPlane v, float tolerance = UnrealMath.KindaSmallNumber) => Abs(X - v.X) <= tolerance && Abs(Y - v.Y) <= tolerance && Abs(Z - v.Z) <= tolerance && Abs(W - v.W) <= tolerance;

        public override bool Equals(object? obj) => obj is FPlane other && Equals(other, 0f);
    }
}

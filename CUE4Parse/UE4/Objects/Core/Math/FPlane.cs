using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static System.MathF;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// USE Ar.Read<FPlane> FOR FLOATS AND new FPlane(Ar) FOR DOUBLES
    /// </summary>
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

        public FPlane(float x, float y, float z, float w) : this()
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public FPlane(TIntVector3<float> vector, float w) : this()
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            W = w;
        }

        public FPlane(TIntVector3<double> vector, double w) : this()
        {
            X = (float) vector.X;
            Y = (float) vector.Y;
            Z = (float) vector.Z;
            W = (float) w;
        }

        public FPlane(FArchive Ar)
        {
            Vector = new FVector(Ar);
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                W = (float) Ar.Read<double>();
            }
            else
            {
                W = Ar.Read<float>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float PlaneDot(FVector p) => X * p.X + Y * p.Y + Z * p.Z - W;

        public bool Equals(FPlane v, float tolerance = UnrealMath.KindaSmallNumber) => Abs(X - v.X) <= tolerance && Abs(Y - v.Y) <= tolerance && Abs(Z - v.Z) <= tolerance && Abs(W - v.W) <= tolerance;

        public override bool Equals(object? obj) => obj is FPlane other && Equals(other, 0f);
    }
}

using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// USE Ar.Read<FVector2D> FOR FLOATS AND new FVector2D(Ar) FOR DOUBLES
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FVector2D : IUStruct,
        IMultiplyOperators<FVector2D,FVector2D,FVector2D>, 
        IMultiplyOperators<FVector2D,float,FVector2D>, 
        ISubtractionOperators<FVector2D,FVector2D,FVector2D>,
        ISubtractionOperators<FVector2D,float,FVector2D>,
        IAdditionOperators<FVector2D,FVector2D,FVector2D>,
        IAdditionOperators<FVector2D,float,FVector2D>
    {
        public static readonly FVector2D ZeroVector = new(0, 0);

        public readonly float X;
        public readonly float Y;

        public FVector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public FVector2D(FArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                X = (float) Ar.Read<double>();
                Y = (float) Ar.Read<double>();
            }
            else
            {
                X = Ar.Read<float>();
                Y = Ar.Read<float>();
            }
        }
        
        public static FVector2D operator +(FVector2D a, FVector2D b) => new(a.X + b.X, a.Y + b.Y);
        public static FVector2D operator -(FVector2D a, FVector2D b) => new(a.X - b.X, a.Y - b.Y);
        public static FVector2D operator *(FVector2D a, FVector2D b) => new(a.X * b.X, a.Y * b.Y);
        public static FVector2D operator /(FVector2D a, FVector2D b) => new(a.X / b.X, a.Y / b.Y);

        public static FVector2D operator +(FVector2D a, float b) => new(a.X + b, a.Y + b);
        public static FVector2D operator *(FVector2D a, float b) => new(a.X * b, a.Y * b);
        public static FVector2D operator -(FVector2D a, float b) => new(a.X - b, a.Y - b);

        public override string ToString() => $"X={X,3:F3} Y={Y,3:F3}";
    }
}

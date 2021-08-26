using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// A 4D homogeneous vector, 4x1 FLOATs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FVector4 : IUStruct
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;

        public static readonly FVector4 ZeroVector = new(0, 0, 0, 0);

        public FVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        /// <summary>
        /// Constructor from 3D Vector and W
        /// </summary>
        /// <param name="v">3D Vector to set first three components.</param>
        /// <param name="w">W Coordinate.</param>
        public FVector4(FVector v, float w = 1f) : this(v.X, v.Y, v.Z, w) { }

        public FVector4(FLinearColor color) : this(color.R, color.G, color.B, color.A) { }

        public override string ToString() => $"X={X,3:F3} Y={Y,3:F3} Z={Z,3:F3} W={W,3:F3}";
    }
}
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct FPlane
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
        
        public bool Equals(FPlane v, float tolerance = FVector.KindaSmallNumber) => System.Math.Abs(X - v.X) <= tolerance &&
                                                                             System.Math.Abs(Y - v.Y) <= tolerance &&
                                                                             System.Math.Abs(Z - v.Z) <= tolerance &&
                                                                             System.Math.Abs(W - v.W) <= tolerance;
        
        public override bool Equals(object? obj) => obj is FPlane other && Equals(other, 0f);
    }
}
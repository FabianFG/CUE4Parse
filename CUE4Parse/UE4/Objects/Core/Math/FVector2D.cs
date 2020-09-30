using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FVector2D : IUStruct
    {
        public readonly float X;
        public readonly float Y;

        public FVector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"X={X:###.###} Y={Y:###.###}";
    }
}
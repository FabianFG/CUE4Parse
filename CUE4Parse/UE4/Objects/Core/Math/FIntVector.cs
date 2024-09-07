using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIntVector : IUStruct
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public static FIntVector Zero => new FIntVector(0, 0, 0);

        public FIntVector(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
    }
}

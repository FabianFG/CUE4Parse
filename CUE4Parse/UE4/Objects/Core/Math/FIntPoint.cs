using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIntPoint : IUStruct
    {
        public readonly int X;
        public readonly int Y;

        public FIntPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIntRect : IUStruct
    {
        public readonly FIntPoint Min;
        public readonly FIntPoint Max;
        
        public FIntRect(FIntPoint min, FIntPoint max)
        {
            Min = min;
            Max = max;
        }
    }
}
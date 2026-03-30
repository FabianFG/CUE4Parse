using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FIntVector2
{
    public readonly int X;
    public readonly int Y;

    public FIntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }
}

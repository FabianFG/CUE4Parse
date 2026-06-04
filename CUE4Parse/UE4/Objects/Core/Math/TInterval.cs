using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public struct TInterval<T> : IUStruct
{
    public T Min;
    public T Max;

    public TInterval(T min, T max)
    {
        Min = min;
        Max = max;
    }

    public override string ToString()
    {
        return $"{nameof(Min)}: {Min}, {nameof(Max)}: {Max}";
    }
}

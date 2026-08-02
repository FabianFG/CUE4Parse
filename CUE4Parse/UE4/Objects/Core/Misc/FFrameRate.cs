using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc;

[StructFallback]
[StructLayout(LayoutKind.Sequential)]
public readonly struct FFrameRate : IUStruct
{
    public readonly int Numerator;
    public readonly int Denominator;

    public FFrameRate(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public FFrameRate(FStructFallback fallback)
    {
        Numerator = fallback.GetOrDefault<int>(nameof(Numerator));
        Denominator = fallback.GetOrDefault<int>(nameof(Denominator));
    }

    public override string ToString() => $"{nameof(Numerator)}: {Numerator}, {nameof(Denominator)}: {Denominator}";
}

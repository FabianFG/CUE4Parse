using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Misc;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FFrameNumber : IUStruct
{
    public readonly int Value;

    public FFrameNumber(int value) => Value = value;

    public override string ToString() => Value.ToString();

    public static implicit operator FFrameNumber(float value) => new((int) value);
}

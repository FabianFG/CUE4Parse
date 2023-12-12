using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TRange<T> : IUStruct, ISerializable where T : ISerializable
{
    /** Holds the range's lower bound. */
    public readonly TRangeBound<T> LowerBound;
    /** Holds the range's upper bound. */
    public readonly TRangeBound<T> UpperBound;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(LowerBound);
        Ar.Serialize(UpperBound);
    }

    public override string ToString()
    {
        return $"{nameof(LowerBound)}: {LowerBound}, {nameof(UpperBound)}: {UpperBound}";
    }
}
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TRange<T> : IUStruct
    {
        /** Holds the range's lower bound. */
        public readonly TRangeBound<T> LowerBound;
        /** Holds the range's upper bound. */
        public readonly TRangeBound<T> UpperBound;

        public override string ToString()
        {
            return $"{nameof(LowerBound)}: {LowerBound}, {nameof(UpperBound)}: {UpperBound}";
        }
    }
}

using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public enum ERangeBoundTypes : sbyte
    {
        /** The range excludes the bound. */
        Exclusive,

        /** The range includes the bound. */
        Inclusive,

        /** The bound is open. */
        Open
    };

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TRangeBound<T> : IUStruct
    {
        /** Holds the type of the bound. */
        public readonly ERangeBoundTypes BoundType;
        /** Holds the bound's value. */
        public readonly T BoundValue;
    }
}

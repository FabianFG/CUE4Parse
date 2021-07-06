using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /**
     * Enumerates the valid types of range bounds.
     */
    public enum ERangeBoundTypes : byte
    {
        /** The range excludes the bound. */
        Exclusive,

        /** The range includes the bound. */
        Inclusive,

        /** The bound is open. */
        Open
    }

    /**
     * Template for range bounds.
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct TRangeBound<T> : IUStruct
    {
        /** Holds the type of the bound. */
        public readonly ERangeBoundTypes Type;

        /** Holds the bound's value. */
        public readonly T Value;

        public override string ToString() => Value?.ToString() ?? string.Empty;
    }
}
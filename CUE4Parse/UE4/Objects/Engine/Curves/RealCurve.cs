using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    /** Method of interpolation between this key and the next. */
    public enum ERichCurveInterpMode : byte
    {
        /** Use linear interpolation between values. */
        RCIM_Linear,
        /** Use a constant value. Represents stepped values. */
        RCIM_Constant,
        /** Cubic interpolation. See TangentMode for different cubic interpolation options. */
        RCIM_Cubic,
        /** No interpolation. */
        RCIM_None
    }

    /** Enumerates extrapolation options. */
    public enum ERichCurveExtrapolation : byte
    {
        /** Repeat the curve without an offset. */
        RCCE_Cycle,
        /** Repeat the curve with an offset relative to the first or last key's value. */
        RCCE_CycleWithOffset,
        /** Sinusoidally extrapolate. */
        RCCE_Oscillate,
        /** Use a linearly increasing value for extrapolation.*/
        RCCE_Linear,
        /** Use a constant value for extrapolation */
        RCCE_Constant,
        /** No Extrapolation */
        RCCE_None
    }

    /** A rich, editable float curve */
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRealCurve : IUStruct
    {
        public readonly float DefaultValue;
        public readonly ERichCurveExtrapolation PreInfinityExtrap;
        public readonly ERichCurveExtrapolation PostInfinityExtrap;
    }
}
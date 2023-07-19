using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveExtrapolation;

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
    [StructFallback]
    public abstract class FRealCurve : IUStruct
    {
        public float DefaultValue;
        [JsonConverter(typeof(StringEnumConverter))]
        public ERichCurveExtrapolation PreInfinityExtrap;
        [JsonConverter(typeof(StringEnumConverter))]
        public ERichCurveExtrapolation PostInfinityExtrap;

        public FRealCurve()
        {
            DefaultValue = 3.402823466e+38f; // MAX_flt
            PreInfinityExtrap = RCCE_Constant;
            PostInfinityExtrap = RCCE_Constant;
        }

        public FRealCurve(FStructFallback data)
        {
            DefaultValue = data.GetOrDefault(nameof(DefaultValue), 3.402823466e+38f);
            PreInfinityExtrap = data.GetOrDefault(nameof(PreInfinityExtrap), RCCE_Constant);
            PostInfinityExtrap = data.GetOrDefault(nameof(PostInfinityExtrap), RCCE_Constant);
        }

        public abstract void RemapTimeValue(ref float inTime, ref float cycleValueOffset);
        public abstract float Eval(float inTime, float inDefaultTime = 0);

        protected static void CycleTime(float minTime, float maxTime, ref float inTime, ref int cycleCount)
        {
            var initTime = inTime;
            var duration = maxTime - minTime;

            if (inTime > maxTime)
            {
                cycleCount = (int) ((maxTime - inTime) / duration);
                inTime += duration * cycleCount;
            }
            else if (inTime < minTime)
            {
                cycleCount = (int) ((inTime - minTime) / duration);
                inTime -= duration * cycleCount;
            }

            if (inTime == maxTime && initTime < minTime)
                inTime = minTime;
            if (inTime == minTime && initTime > maxTime)
                inTime = maxTime;

            cycleCount = Math.Abs(cycleCount);
        }
    }
}

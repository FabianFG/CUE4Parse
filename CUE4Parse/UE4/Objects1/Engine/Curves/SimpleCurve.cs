using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using static CUE4Parse.UE4.Objects.Engine.Curves.ERichCurveInterpMode;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FSimpleCurveKey : IUStruct
    {
        public readonly float Time;
        public readonly float Value;
    }

    [StructFallback]
    public class FSimpleCurve : FRealCurve
    {
        public ERichCurveInterpMode InterpMode;
        public FSimpleCurveKey[] Keys;

        public FSimpleCurve()
        {
            InterpMode = RCIM_Linear;
            Keys = Array.Empty<FSimpleCurveKey>();
        }

        public FSimpleCurve(FStructFallback data)
        {
            InterpMode = data.GetOrDefault(nameof(InterpMode), RCIM_Linear);
            Keys = data.GetOrDefault(nameof(Keys), Array.Empty<FSimpleCurveKey>());
        }

        public override void RemapTimeValue(ref float inTime, ref float cycleValueOffset)
        {
            var numKeys = Keys.Length;
            if (numKeys < 2) return;

            if (inTime <= Keys[0].Time)
            {
                if (PreInfinityExtrap != ERichCurveExtrapolation.RCCE_Linear && PreInfinityExtrap != ERichCurveExtrapolation.RCCE_Constant)
                {
                    var minTime = Keys[0].Time;
                    var maxTime = Keys[numKeys - 1].Time;

                    var cycleCount = 0;
                    CycleTime(minTime, maxTime, ref inTime, ref cycleCount);

                    if (PreInfinityExtrap == ERichCurveExtrapolation.RCCE_CycleWithOffset)
                    {
                        var dv = Keys[0].Value - Keys[numKeys - 1].Value;
                        cycleValueOffset = dv * cycleCount;
                    }
                    else if (PreInfinityExtrap == ERichCurveExtrapolation.RCCE_Oscillate)
                    {
                        if (cycleCount % 2 == 1)
                        {
                            inTime = minTime + (maxTime - inTime);
                        }
                    }
                }
            }
            else if (inTime >= Keys[numKeys - 1].Time)
            {
                if (PostInfinityExtrap != ERichCurveExtrapolation.RCCE_Linear && PostInfinityExtrap != ERichCurveExtrapolation.RCCE_Constant)
                {
                    var minTime = Keys[0].Time;
                    var maxTime = Keys[numKeys - 1].Time;

                    var cycleCount = 0;
                    CycleTime(minTime, maxTime, ref inTime, ref cycleCount);

                    if (PostInfinityExtrap == ERichCurveExtrapolation.RCCE_CycleWithOffset)
                    {
                        var dv = Keys[numKeys - 1].Value - Keys[0].Value;
                        cycleValueOffset = dv * cycleCount;
                    }
                    else if (PostInfinityExtrap == ERichCurveExtrapolation.RCCE_Oscillate)
                    {
                        if (cycleCount % 2 == 1)
                        {
                            inTime = minTime + (maxTime - inTime);
                        }
                    }
                }
            }
        }

        public override float Eval(float inTime, float inDefaultValue = 0)
        {
            // Remap time if extrapolation is present and compute offset value to use if cycling
            var cycleValueOffset = 0f;
            var inTimeRef = inTime;
            var cycleValueOffsetRef = cycleValueOffset;
            RemapTimeValue(ref inTimeRef, ref cycleValueOffsetRef);
            inTime = inTimeRef;
            cycleValueOffset = cycleValueOffsetRef;

            var numKeys = Keys.Length;

            // If the default value hasn't been initialized, use the incoming default value
            var interpVal = DefaultValue == float.MaxValue ? inDefaultValue : DefaultValue;

            if (numKeys == 0)
            {
                //
            }
            else if (numKeys < 2 || inTime <= Keys[0].Time)
            {
                if (PreInfinityExtrap == ERichCurveExtrapolation.RCCE_Linear && numKeys > 1)
                {
                    var dt = Keys[1].Time - Keys[0].Time;

                    if (Math.Abs(dt) <= UnrealMath.SmallNumber)
                    {
                        interpVal = Keys[0].Value;
                    }
                    else
                    {
                        var dv = Keys[1].Value - Keys[0].Value;
                        var slope = dv / dt;

                        interpVal = slope * (inTime - Keys[0].Time) + Keys[0].Value;
                    }
                }
                else
                {
                    // Otherwise if constant or in a cycle or oscillate, always use the first key value
                    interpVal = Keys[0].Value;
                }
            }
            else if (inTime < Keys[numKeys - 1].Time)
            {
                // perform a lower bound to get the second of the interpolation nodes
                var first = 1;
                var last = numKeys - 1;
                var count = last - first;

                while (count > 0)
                {
                    var step = count / 2;
                    var middle = first + step;

                    if (inTime >= Keys[middle].Time)
                    {
                        first = middle + 1;
                        count -= step + 1;
                    }
                    else
                    {
                        count = step;
                    }
                }

                interpVal = EvalForTwoKeys(Keys[first - 1], Keys[first], inTime);
            }
            else
            {
                if (PostInfinityExtrap == ERichCurveExtrapolation.RCCE_Linear)
                {
                    var dt = Keys[numKeys - 2].Time - Keys[numKeys - 1].Time;

                    if (Math.Abs(dt) <= UnrealMath.SmallNumber)
                    {
                        interpVal = Keys[numKeys - 1].Value;
                    }
                    else
                    {
                        var dv = Keys[numKeys - 2].Value - Keys[numKeys - 1].Value;
                        var slope = dv / dt;

                        interpVal = slope * (inTime - Keys[numKeys - 1].Time) + Keys[numKeys - 1].Value;
                    }
                }
                else
                {
                    // Otherwise if constant or in a cycle or oscillate, always use the last key value
                    interpVal = Keys[numKeys - 1].Value;
                }
            }

            return interpVal + cycleValueOffset;
        }

        private float EvalForTwoKeys(FSimpleCurveKey key1, FSimpleCurveKey key2, float inTime)
        {
            var diff = key2.Time - key1.Time;

            if (diff > 0f && InterpMode != RCIM_Constant)
            {
                var alpha = (inTime - key1.Time) / diff;
                var p0 = key1.Value;
                var p3 = key2.Value;

                return MathUtils.Lerp(p0, p3, alpha);
            }

            return key1.Value;
        }
    }
}

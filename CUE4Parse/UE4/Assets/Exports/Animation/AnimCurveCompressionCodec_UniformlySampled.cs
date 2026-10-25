using System.Collections;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using GenericReader;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCurveCompressionCodec_UniformlySampled : UAnimCurveCompressionCodec
{
    public override FFloatCurve[] ConvertCurves(FSmartName[] names, byte[] data)
    {
        if (names.Length == 0)
            return [];

        using var Ar = new GenericBufferReader(data);
        var numCurves = names.Length;
        var numConstantCurves = Ar.Read<int>();
        var numAnimatedCurves = numCurves - numConstantCurves;
        var numSamples = Ar.Read<int>();
        if (numSamples == 0)
            return [];

        var sampleRate = Ar.Read<float>();

        var constantCurvesBitset = new BitArray(Ar.ReadArray<byte>(sizeof(uint) * ((numCurves + 31) / 32)));
        var constantSamplesSpan = Ar.ReadSpan<float>(numConstantCurves);
        var animatedSamplesSpan = Ar.ReadSpan<float>(numAnimatedCurves * numSamples);

        var timeValues = new float[numSamples];
        for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
        {
            timeValues[sampleIndex] = sampleIndex / sampleRate;
        }

        var floatCurves = new FFloatCurve[numCurves];
        for (int curveIndex = 0, constantCurveIndex = 0, animatedCurveIndex = 0; curveIndex < numCurves; curveIndex++)
        {
            var curveKeys = animatedSamplesSpan.Slice(animatedCurveIndex * numSamples, numSamples);

            var floatCurve = new FFloatCurve
            {
                CurveName = names[curveIndex].DisplayName,
                FloatCurve = new FRichCurve
                {
                    Keys = new FRichCurveKey[numSamples]
                }
            };

            var bIsConstant = constantCurvesBitset.Get(curveIndex);
            if (bIsConstant)
            {
                for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
                {
                    floatCurve.FloatCurve.Keys[sampleIndex] = new FRichCurveKey
                    {
                        Value = constantSamplesSpan[constantCurveIndex],
                        Time = timeValues[sampleIndex]
                    };
                }

                constantCurveIndex++;
            }
            else
            {
                for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
                {
                    floatCurve.FloatCurve.Keys[sampleIndex] = new FRichCurveKey
                    {
                        Value = curveKeys[sampleIndex],
                        Time = timeValues[sampleIndex]
                    };
                }
                animatedCurveIndex++;
            }

            floatCurves[curveIndex] = floatCurve;
        }

        return floatCurves;
    }
}

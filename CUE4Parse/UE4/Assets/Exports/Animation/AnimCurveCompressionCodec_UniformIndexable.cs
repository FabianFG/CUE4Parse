using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using GenericReader;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCurveCompressionCodec_UniformIndexable : UAnimCurveCompressionCodec
{
    public override FFloatCurve[] ConvertCurves(FSmartName[] names, byte[] data)
    {
        if (names.Length == 0)
            return [];

        using var Ar = new GenericBufferReader(data);
        var numCurves = names.Length;
        var numSamples = Ar.Read<int>();
        if (numSamples == 0)
            return [];
        var sampleRate = Ar.Read<float>();
        var floatKeys = Ar.ReadSpan<float>(numSamples * numCurves);

        return ExtractUniformFloatCurves(names, numCurves, floatKeys, numSamples, sampleRate);
    }

    public static FFloatCurve[] ExtractUniformFloatCurves(FSmartName[] names, int numCurves, ReadOnlySpan<float> floatKeys, int numSamples, float sampleRate)
    {
        var timeValues = new float[numSamples];
        for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
        {
            timeValues[sampleIndex] = sampleIndex / sampleRate;
        }

        var floatCurves = new FFloatCurve[numCurves];
        for (var curveIndex = 0; curveIndex < numCurves; curveIndex++)
        {
            var curveKeys = floatKeys.Slice(curveIndex * numSamples, numSamples);

            var floatCurve = new FFloatCurve
            {
                CurveName = names[curveIndex].DisplayName,
                FloatCurve = new FRichCurve
                {
                    Keys = new FRichCurveKey[numSamples]
                }
            };

            for (var sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                floatCurve.FloatCurve.Keys[sampleIndex] = new FRichCurveKey
                {
                    Value = curveKeys[sampleIndex],
                    Time = timeValues[sampleIndex]
                };
            }

            floatCurves[curveIndex] = floatCurve;
        }

        return floatCurves;
    }
}

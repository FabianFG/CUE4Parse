using System.Runtime.InteropServices;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL;

public class AnimCurveCompressionCodec_ACL : UAnimCurveCompressionCodec
{
    public override unsafe FFloatCurve[] ConvertCurves(FSmartName[] names, byte[] data)
    {
        using var compressedTracks = new CompressedTracks(data);
        var header = compressedTracks.GetTracksHeader();
        var numSamples = (int) header.NumSamples;
        var numTracks = (int) header.NumTracks;

        if (numTracks != names.Length)
        {
            Log.Warning("ACL curve track count {NumTracks} does not match curve name count {NumNames}", numTracks, names.Length);
        }

        var floatKeys = new float[numTracks * numSamples];
        if (floatKeys.Length > 0)
        {
            fixed (float* floatKeysPtr = floatKeys)
            {
                nReadCurveACLData(compressedTracks.Handle, floatKeysPtr);
            }
        }

        var numCurves = Math.Min(numTracks, names.Length);
        var floatCurves = new FFloatCurve[numCurves];
        for (var curveIndex = 0; curveIndex < numCurves; curveIndex++)
        {
            var offset = curveIndex * numSamples;
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
                    Value = floatKeys[offset + sampleIndex],
                    Time = sampleIndex / header.SampleRate
                };
            }

            floatCurves[curveIndex] = floatCurve;
        }

        return floatCurves;
    }

    [DllImport(ACLNative.LIB_NAME)]
    private static extern unsafe void nReadCurveACLData(IntPtr compressedTracks, float* outFloatKeys);
}

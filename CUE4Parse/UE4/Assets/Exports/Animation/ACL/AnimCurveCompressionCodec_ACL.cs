using System;
using System.Runtime.InteropServices;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL;

public class AnimCurveCompressionCodec_ACL : UAnimCurveCompressionCodec
{
    public override unsafe FFloatCurve[] ConvertCurves(UAnimSequence animSeq)
    {
        var curveNames = animSeq.CompressedCurveNames;
        var numCurves = curveNames.Length;

        if (numCurves == 0 || animSeq.CompressedCurveByteStream is null)
        {
            return [];
        }

        var compressedTracks = new CompressedTracks(animSeq.CompressedCurveByteStream);
        var header = compressedTracks.GetTracksHeader();
        var numSamples = header.NumSamples;

        var floatKeys = new float[numCurves * numSamples];
        fixed (float* floatKeysPtr = floatKeys)
        {
            nReadCurveACLData(compressedTracks.Handle, floatKeysPtr);
        }
        
        var floatCurves = new FFloatCurve[numCurves];
        for (var curveIndex = 0; curveIndex < numCurves; curveIndex++)
        {
            var curveKeys = new float[numSamples];
            var offset = curveIndex * numSamples;
            Array.Copy(floatKeys, offset, curveKeys, 0, numSamples);

            var floatCurve = new FFloatCurve
            {
                CurveName = animSeq.CompressedCurveNames[curveIndex].DisplayName,
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
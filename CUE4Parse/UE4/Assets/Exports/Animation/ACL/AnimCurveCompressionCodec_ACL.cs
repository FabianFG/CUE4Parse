using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL;

public class AnimCurveCompressionCodec_ACL : UAnimCurveCompressionCodec
{
    public float CurvePrecision;
    public float MorphTargetPositionPrecision;

    public uint ForceRebuildVersion;
    public uint SettingsHash;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CurvePrecision = Ar.Read<float>();
        MorphTargetPositionPrecision = Ar.Read<float>();

        ForceRebuildVersion = Ar.Read<uint>();
        SettingsHash = Ar.Read<uint>();
    }

    public override FFloatCurve[] ConvertCurves(UAnimSequence animSeq)
    {
        var indexedCurveNames = animSeq.CompressedCurveNames;
        var numCurves = indexedCurveNames.Length;

        if (numCurves == 0 || animSeq.CompressedCurveByteStream is null)
        {
            return [];
        }

        var compressedTracks = new CompressedTracks(animSeq.CompressedCurveByteStream);
        var header = compressedTracks.GetTracksHeader();

        var rawKeys = new float[header.NumSamples][];
        for (var sampleIndex = 0; sampleIndex < header.NumSamples; sampleIndex++)
        {
            var floatKeys = new float[numCurves];
            unsafe
            {
                fixed (float* floatKeysPtr = floatKeys)
                {
                    nReadCurveACLData(compressedTracks.Handle, numCurves, sampleIndex, floatKeysPtr);
                }
            }

            rawKeys[sampleIndex] = floatKeys;
        }

        var floatCurves = new FFloatCurve[numCurves];
        for (var curveIndex = 0; curveIndex < numCurves; curveIndex++)
        {
            var floatCurve = new FFloatCurve
            {
                CurveName = animSeq.CompressedCurveNames[curveIndex].DisplayName,
                FloatCurve = new FRichCurve
                {
                    Keys = new FRichCurveKey[header.NumSamples]
                }
            };
            
            for (var sampleIndex = 0; sampleIndex < header.NumSamples; sampleIndex++)
            {
                var floatValue = rawKeys[sampleIndex][curveIndex];
                floatCurve.FloatCurve.Keys[sampleIndex] = new FRichCurveKey
                {
                    Value = floatValue,
                    Time = sampleIndex / header.SampleRate
                };
            }

            floatCurves[curveIndex] = floatCurve;

        }
        return floatCurves;
    }
    
    [DllImport(ACLNative.LIB_NAME)]
    private static extern unsafe void nReadCurveACLData(IntPtr compressedTracks, int sampleIndex, int targetSampleIndex, float* outFloatKeys);
}
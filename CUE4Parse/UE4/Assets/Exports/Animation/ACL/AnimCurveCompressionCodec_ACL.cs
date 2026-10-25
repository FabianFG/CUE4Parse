using System.Runtime.InteropServices;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Objects.Engine.Animation;

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

        return UAnimCurveCompressionCodec_UniformIndexable.ExtractUniformFloatCurves(names, Math.Min(numTracks, names.Length),
            floatKeys, numSamples, header.SampleRate);
    }

    [DllImport(ACLNative.LIB_NAME)]
    private static extern unsafe void nReadCurveACLData(IntPtr compressedTracks, float* outFloatKeys);
}

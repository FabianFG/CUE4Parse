using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    public class AEFPerTrackCompressionCodec : AnimEncoding
    {
        public static void GetBoneAtomRotation(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;
            var trackData = animData.CompressedTrackOffsets;
            var trackDataStartIndex = trackIndex * 2;
            var rotKeysOffset = trackData[trackDataStartIndex + 1];
            if (rotKeysOffset != -1)
            {
                AnimEncodingUtil.DecomposeHeader(
                    0,
                    out AnimationCompressionFormat keyFormat,
                    out int numKeys,
                    out int formatFlags,
                    out int bytesPerKey,
                    out int fixedBytes);


            }
        }
    }
}
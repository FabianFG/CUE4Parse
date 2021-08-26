using System;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    internal class AEFConstantKeyLerp : AnimEncodingLegacyBase
    {
        private readonly AnimationCompressionFormat _format;

        public AEFConstantKeyLerp(AnimationCompressionFormat format) => _format = format;
        public override void GetBoneAtomRotation(FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            throw new NotImplementedException();
        }

        public override void GetBoneAtomTranslation(FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;

            var trackData = animData.CompressedTrackOffsets;
            var trackDataStartIndex = trackIndex * 4;
            var transKeysOffset = trackData[trackDataStartIndex];
            var numTransKeys = trackData[trackDataStartIndex + 1];

            float alpha = AnimEncodingUtil.TimeToIndex(
                decompContext.SequenceLength,
                decompContext.RelativePos,
                numTransKeys,
                decompContext.Interpolation,
                out var index0,
                out var index1);

            if (index0 != index1)
            {
               
            }
        }

        public override void GetBoneAtomScale(FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            throw new NotImplementedException();
        }
    }
}
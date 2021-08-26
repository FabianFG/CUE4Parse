using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    /*
     * Base class for all bone compression codecs.
     */
    public abstract class UAnimBoneCompressionCodec : UObject
    {
        /** Decompresses all the specified bone tracks. */
        public abstract void DecompressPose(FAnimSequenceDecompressionContext decompContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms);

        /** Decompress a single bone. */
        public abstract void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom);
    }
}
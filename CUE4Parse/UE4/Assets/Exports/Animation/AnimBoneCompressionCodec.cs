namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public struct BoneTrackPair // TODO move to AnimEncoding.cs
    {
        public int AtomIndex;
        public int TrackIndex;
    }

    /**
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
namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    using System.Collections.Generic;

    /**
     * Base class for all bone compression codecs.
     */
    public abstract class UAnimBoneCompressionCodec : UObject
    {
        /** Decompress a single bone. */
        public abstract void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom);

        public abstract void DecompressPose(FAnimSequenceDecompressionContext decompContext, List<BoneTrackPair>)
    }
}
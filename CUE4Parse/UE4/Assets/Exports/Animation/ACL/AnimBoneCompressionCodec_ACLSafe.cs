using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public class UAnimBoneCompressionCodec_ACLSafe : UAnimBoneCompressionCodec_ACLBase
    {
        public override void DecompressPose(FAnimSequenceDecompressionContext decompContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms)
        {
            throw new System.NotImplementedException();
        }

        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            throw new System.NotImplementedException();
        }
    }
}
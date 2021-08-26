using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimCompress : UAnimBoneCompressionCodec
    {
        public override void DecompressPose(FAnimSequenceDecompressionContext decompContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms)
        {
            throw new System.NotImplementedException();
        }

        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            // Initialize to identity to set the scale and in case of a missing rotation or translation codec
            outAtom = FTransform.Identity;

            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;
            using var ar = new FByteArchive("BoneDataReader", animData.CompressedByteStream);
            // decompress the translation component using the proper method
            ((AnimEncodingLegacyBase)animData.TranslationCodec).GetBoneAtomTranslation(ar, decompContext, trackIndex, ref outAtom);

            // decompress the rotation component using the proper method
            ((AnimEncodingLegacyBase)animData.RotationCodec).GetBoneAtomRotation(ar, decompContext, trackIndex, ref outAtom);

            // we assume scale keys can be empty, so only extract if we have valid keys
            if (animData.CompressedScaleOffsets.IsValid())
            {
                // decompress the rotation component using the proper method
                ((AnimEncodingLegacyBase)animData.RotationCodec).GetBoneAtomScale(ar, decompContext, trackIndex, ref outAtom);
            }
        }
    }
}
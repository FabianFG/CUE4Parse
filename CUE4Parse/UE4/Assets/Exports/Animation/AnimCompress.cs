namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimCompress : UAnimBoneCompressionCodec
    {
        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            // Initialize to identity to set the scale and in case of a missing rotation or translation codec
            outAtom = FTransform.Identity; // .SetIdentity()

            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;

            // decompress the translation component using the proper method
            /*((AnimEncodingLegacyBase*)AnimData.TranslationCodec)->GetBoneAtomTranslation(outAtom, DecompContext, TrackIndex);

            // decompress the rotation component using the proper method
            ((AnimEncodingLegacyBase*)AnimData.RotationCodec)->GetBoneAtomRotation(outAtom, DecompContext, TrackIndex);

            // we assume scale keys can be empty, so only extract if we have valid keys
            if (AnimData.CompressedScaleOffsets.IsValid())
            {
                // decompress the rotation component using the proper method
                ((AnimEncodingLegacyBase*)AnimData.ScaleCodec)->GetBoneAtomScale(outAtom, DecompContext, TrackIndex);
            }*/
        }
    }
}
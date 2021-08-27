using CUE4Parse.UE4.Assets.Exports.Animation.Codec;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimCompress_PerTrackCompression : UAnimCompress
    {
        private static readonly AEFPerTrackCompressionCodec _staticCodec = new();

        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            // Initialize to identity to set the scale and in case of a missing rotation or translation codec
            outAtom = FTransform.Identity;

            var animData = (FUECompressedAnimData) decompContext.CompressedAnimData;
            using var Ar = new FByteArchive("BoneDataReader", animData.CompressedByteStream);

            // decompress the translation component using the proper method
            _staticCodec.GetBoneAtomTranslation(Ar, decompContext, trackIndex, ref outAtom);

            // decompress the rotation component using the proper method
            _staticCodec.GetBoneAtomRotation(Ar, decompContext, trackIndex, ref outAtom);

            // we assume scale keys can be empty, so only extract if we have valid keys
            if (animData.CompressedScaleOffsets.IsValid())
            {
                // decompress the rotation component using the proper method
                _staticCodec.GetBoneAtomScale(Ar, decompContext, trackIndex, ref outAtom);
            }
        }
    }
}
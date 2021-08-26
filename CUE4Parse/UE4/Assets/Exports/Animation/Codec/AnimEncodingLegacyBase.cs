using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    abstract class AnimEncodingLegacyBase : IAnimEncoding
    {
        public virtual void GetBoneAtomRotation(
            FArchive ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom){}

        public virtual void GetBoneAtomTranslation(
            FArchive ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom){}

        public virtual void GetBoneAtomScale(
            FArchive ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom){}
    }
}
namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    abstract class AnimEncodingLegacyBase : IAnimEncoding
    {
        public abstract void GetBoneAtomRotation(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom);

        public abstract void GetBoneAtomTranslation(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom);

        public abstract void GetBoneAtomScale(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom);
    }
}
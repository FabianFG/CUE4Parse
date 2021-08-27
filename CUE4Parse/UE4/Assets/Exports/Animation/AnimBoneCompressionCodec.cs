using System.Text;
using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    /**
     * Base class for all bone compression codecs.
     */
    public abstract class UAnimBoneCompressionCodec : UObject
    {
        public UAnimBoneCompressionCodec? GetCodec(string ddcHandle)
        {
            var thisHandle = GetCodecDDCHandle();
            return thisHandle == ddcHandle ? this : null;
        }

        public string GetCodecDDCHandle()
        {
            var handle = new StringBuilder(128);
            handle.Append(Name);

            var obj = Outer;
            while (obj != null && obj is not UAnimBoneCompressionSettings)
            {
                handle.Append('.');
                handle.Append(obj.Name);
                obj = obj.Outer;
            }

            return handle.ToString();
        }

        public abstract ICompressedAnimData AllocateAnimData();

        /** Decompresses all the specified bone tracks. */
        public abstract void DecompressPose(FAnimSequenceDecompressionContext decompContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms);

        /** Decompress a single bone. */
        public abstract void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom);
    }
}
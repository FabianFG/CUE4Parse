using System.Text;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    /**
     * Base class for all bone compression codecs.
     */
    public abstract class UAnimBoneCompressionCodec : UObject
    {
        public virtual UAnimBoneCompressionCodec? GetCodec(string ddcHandle)
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
    }
}
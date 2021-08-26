using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public enum EAnimInterpolationType
    {
        Linear,
        Step
    }

    public class FAnimSequenceDecompressionContext
    {
        // Anim info
        public float SequenceLength;
        public EAnimInterpolationType Interpolation;
        public FName AnimName;

        public ICompressedAnimData CompressedAnimData;
        public float Time;
        public float RelativePos;

        public FAnimSequenceDecompressionContext(float sequenceLength, EAnimInterpolationType interpolation, FName animName, ICompressedAnimData compressedAnimData)
        {
            SequenceLength = sequenceLength;
            Interpolation = interpolation;
            AnimName = animName;
            CompressedAnimData = compressedAnimData;
        }

        public void Seek(float sampleAtTime)
        {
            Time = sampleAtTime;
            RelativePos = sampleAtTime / SequenceLength;
        }
    }
}
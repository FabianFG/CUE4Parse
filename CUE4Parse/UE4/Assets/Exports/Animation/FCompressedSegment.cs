using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FCompressedSegment
    {
        public int StartFrame;
        public int NumFrames;
        public int ByteStreamOffset;
        public AnimationCompressionFormat TranslationCompressionFormat;
        public AnimationCompressionFormat RotationCompressionFormat;
        public AnimationCompressionFormat ScaleCompressionFormat;
    }
}
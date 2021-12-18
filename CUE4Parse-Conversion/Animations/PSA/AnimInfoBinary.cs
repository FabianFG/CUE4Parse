using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.ActorX;

namespace CUE4Parse_Conversion.Animations.PSA
{
    /**
     * Binary animation info format - used to organize raw animation keys into FAnimSeqs on rebuild
     * Similar to MotionChunkDigestInfo.
     */
    public class AnimInfoBinary
    {
        /** Animation's name */
        public string Name;
        /** Animation's group name */
        public string Group;

        /** TotalBones * NumRawFrames is number of animation keys to digest. */
        public int TotalBones;

        /** 0 none 1 included (unused) */
        public int RootInclude;
        /** Reserved: variants in tradeoffs for compression. */
        public int KeyCompressionStyle;
        /** Max key quotum for compression; ActorX sets this to numFrames*numBones */
        public int KeyQuotum;
        /** desired */
        public float KeyReduction;
        /** explicit - can be overridden by the animation rate */
        public float TrackTime;
        /** frames per second. */
        public float AnimRate;
        /** Reserved: for partial animations */
        public int StartBone;
        /** global number of first animation frame */
        public int FirstRawFrame;
        /** NumRawFrames and AnimRate dictate tracktime... */
        public int NumRawFrames;

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(Name, 64);
            Ar.Write(Group, 64);
            Ar.Write(TotalBones);
            Ar.Write(RootInclude);
            Ar.Write(KeyCompressionStyle);
            Ar.Write(KeyQuotum);
            Ar.Write(KeyReduction);
            Ar.Write(TrackTime);
            Ar.Write(AnimRate);
            Ar.Write(StartBone);
            Ar.Write(FirstRawFrame);
            Ar.Write(NumRawFrames);
        }
    }
}

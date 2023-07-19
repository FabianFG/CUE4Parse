using CUE4Parse.UE4.Objects.Core.Misc;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public enum ESectionEvaluationFlags : byte
    {
        /** No special flags - normal evaluation */
        None = 0x00,
        /** Segment resides inside the 'pre-roll' time for the section */
        PreRoll = 0x01,
        /** Segment resides inside the 'post-roll' time for the section */
        PostRoll = 0x02,
    };

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FSectionEvaluationData : IUStruct
    {
        /** The implementation index we should evaluate (index into FMovieSceneEvaluationTrack::ChildTemplates) */
        public readonly int ImplIndex;
        /** A forced time to evaluate this section at */
        public readonly FFrameNumber ForcedTime;
        /** Additional flags for evaluating this section */
        public readonly ESectionEvaluationFlags Flags;
    }
}

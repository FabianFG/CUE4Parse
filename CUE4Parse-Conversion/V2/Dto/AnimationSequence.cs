using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.V2.Dto;

public readonly struct AnimationSequence
{
    public readonly float StartTime;
    public readonly float Duration;
    public readonly int FrameCount;
    public readonly int LoopingCount;
    public readonly IList<AnimationTrack> Tracks = [];

    public float EndTime => StartTime + Duration;
    public float FrameRate => FrameCount / Duration;

    public AnimationSequence(UAnimSequence sequence, float startTime = 0.0f, int loopingCount = 1)
    {
        StartTime = startTime;
        Duration = sequence.SequenceLength * sequence.RateScale;
        FrameCount = sequence.NumFrames;
        LoopingCount = loopingCount;
    }
}

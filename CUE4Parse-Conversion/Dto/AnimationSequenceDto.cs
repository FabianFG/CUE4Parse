using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Dto;

public class AnimationSequenceDto : ObjectDto
{
    public readonly float StartTime;
    public readonly float Duration;
    public readonly int FrameCount;
    public readonly int LoopingCount;
    public readonly IList<AnimationTrackDto> Tracks = [];

    public float EndTime => StartTime + Duration;
    public float FrameRate => FrameCount / Duration;

    public AnimationSequenceDto(UAnimSequence sequence, string? name = null, float startTime = 0.0f, int loopingCount = 1) : base(sequence, name)
    {
        StartTime = startTime;
        Duration = sequence.SequenceLength * sequence.RateScale;
        FrameCount = sequence.NumFrames;
        LoopingCount = loopingCount;
    }

    public override void Dispose()
    {
        Tracks.Clear();
    }
}

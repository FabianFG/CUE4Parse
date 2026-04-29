using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.V2.Dto;

public class Animation
{
    public readonly Skeleton Skeleton;
    public readonly IList<AnimationSequence> Sequences = [];

    public readonly float Duration;
    public readonly float StartTime;
    public readonly float PlayRate;

    public Animation(UAnimationAsset animation, float startTime = 0f, float playRate = 1f)
    {
        if (!animation.Skeleton.TryLoad<USkeleton>(out var skeleton))
            throw new ArgumentNullException(nameof(animation), "Animation asset does not have a valid skeleton reference");

        Skeleton = new Skeleton(skeleton);

        switch (animation)
        {
            case UAnimSequence sequence:
            {
                AddSequence(sequence);
                break;
            }
            case UAnimMontage montage:
            {
                foreach (var slotAnimTrack in montage.SlotAnimTracks)
                {
                    foreach (var segment in slotAnimTrack.AnimTrack.AnimSegments)
                    {
                        AddSegment(segment);
                        // seq.Name = slotAnimTrack.SlotName.Text;
                    }
                }
                break;
            }
            case UAnimComposite composite:
            {
                foreach (var segment in composite.AnimationTrack.AnimSegments)
                {
                    AddSegment(segment);
                }
                break;
            }
            default:
                throw new NotSupportedException($"Unsupported animation asset type: {animation.GetType().Name}");
        }

        if (Sequences.Count > 0)
            Duration = Sequences[^1].EndTime;

        StartTime = startTime;
        PlayRate = playRate;
    }

    private void AddSegment(FAnimSegment segment)
    {
        if (!segment.AnimReference.TryLoad<UAnimSequence>(out var sequence))
            return;

        // seq.AnimEndTime = segment.AnimEndTime;
        Sequences.Add(new AnimationSequence(sequence, segment.StartPos, segment.LoopingCount));
    }

    private void AddSequence(UAnimSequence sequence)
    {

    }
}

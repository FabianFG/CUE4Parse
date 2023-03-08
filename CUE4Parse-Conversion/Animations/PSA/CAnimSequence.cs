using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class CAnimSequence
    {
        public UAnimSequence OriginalSequence;
        public readonly FTransform[]? RetargetBasePose;

        public string Name;
        public readonly int NumFrames;
        public readonly float FramesPerSecond;
        public readonly bool IsAdditive;

        public float StartPos;
        public float AnimEndTime;
        public int LoopingCount;
        public List<CAnimTrack> Tracks;

        public CAnimSequence(UAnimSequence animSequence, USkeleton skeleton)
        {
            OriginalSequence = animSequence;
            RetargetBasePose = OriginalSequence.RetargetSource.IsNone switch
            {
                true when OriginalSequence.RetargetSourceAssetReferencePose is { Length: > 0 }
                    => OriginalSequence.RetargetSourceAssetReferencePose,
                false when skeleton.AnimRetargetSources.TryGetValue(OriginalSequence.RetargetSource, out var refPose)
                    => refPose.ReferencePose,
                _ => null
            };

            Name = OriginalSequence.Name;
            NumFrames = OriginalSequence.NumFrames;
            FramesPerSecond = OriginalSequence.NumFrames / OriginalSequence.SequenceLength * MathF.Max(1, OriginalSequence.RateScale);
            IsAdditive = OriginalSequence.AdditiveAnimType != EAdditiveAnimationType.AAT_None;

            StartPos = 0.0f;
            AnimEndTime = OriginalSequence.SequenceLength;
            LoopingCount = 1;
            Tracks = new List<CAnimTrack>(OriginalSequence.GetNumTracks());
        }
    }
}

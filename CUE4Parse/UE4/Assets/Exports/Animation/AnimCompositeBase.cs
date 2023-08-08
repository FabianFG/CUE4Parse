using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public abstract class UAnimCompositeBase : UAnimSequenceBase { }

    [StructFallback]
    public class FAnimSegment
    {
        public FPackageIndex AnimReference;
        public float StartPos;
        public float AnimStartTime;
        public float AnimEndTime;
        public float AnimPlayRate;
        public int LoopingCount;

        public FAnimSegment(FStructFallback fallback)
        {

            AnimReference = fallback.GetOrDefault<FPackageIndex>(nameof(AnimReference));
            StartPos = fallback.GetOrDefault<float>(nameof(StartPos));
            AnimStartTime = fallback.GetOrDefault<float>(nameof(AnimStartTime));
            AnimEndTime = fallback.GetOrDefault<float>(nameof(AnimEndTime));
            AnimPlayRate = fallback.GetOrDefault<float>(nameof(AnimPlayRate));
            LoopingCount = fallback.GetOrDefault<int>(nameof(LoopingCount));
        }

        public float GetValidPlayRate()
        {
            float seqPlayRate = AnimReference.TryLoad(out UAnimSequenceBase sequenceBase) ? sequenceBase.RateScale : 1.0f;
            float finalPlayRate = seqPlayRate * AnimPlayRate;
            return (UnrealMath.IsNearlyZero(finalPlayRate) ? 1.0f : finalPlayRate);
        }

        public float GetLength()
        {
            return (LoopingCount * (AnimEndTime - AnimStartTime)) / Math.Abs(GetValidPlayRate());
        }
    }

    [StructFallback]
    public class FAnimTrack
    {
        public FAnimSegment[] AnimSegments;

        public FAnimTrack(FStructFallback fallback)
        {
            AnimSegments = fallback.GetOrDefault<FAnimSegment[]>(nameof(AnimSegments));
        }

        public float GetLength()
        {
            float totalLength = 0.0f;

            // in the future, if we're more clear about exactly what requirement is for segments,
            // this can be optimized. For now this is slow.
            foreach (var animSegment in AnimSegments)
            {
                var endFrame = animSegment.StartPos + animSegment.GetLength();
                if (endFrame > totalLength)
                {
                    totalLength = endFrame;
                }
            }

            return totalLength;
        }
    }

}

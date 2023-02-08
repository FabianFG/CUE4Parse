using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
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
    }

    [StructFallback]
    public class FAnimTrack
    {
        public FAnimSegment[] AnimSegments;

        public FAnimTrack(FStructFallback fallback)
        {
            AnimSegments = fallback.GetOrDefault<FAnimSegment[]>(nameof(AnimSegments));
        }
    }

}

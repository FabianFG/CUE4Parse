using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

[StructFallback]
public readonly struct FBlendSample
{
    /// <summary>
    /// For linked animations
    /// </summary>
    public readonly FPackageIndex? Animation;

    /// <summary>
    /// blend 0->x, blend 1->y, blend 2->z
    /// </summary>
    public readonly FVector SampleValue;

    public readonly float RateScale;

    public FBlendSample()
    {
        Animation = null;
        SampleValue = FVector.ZeroVector;
        RateScale = 1.0f;
    }

    public FBlendSample(FStructFallback data) : this()
    {
        Animation = data.GetOrDefault<FPackageIndex>(nameof(Animation));
        SampleValue = data.GetOrDefault<FVector>(nameof(SampleValue));
        RateScale = data.GetOrDefault<float>(nameof(RateScale));
    }
}

using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FMovieSceneSegment : IUStruct
{
    public readonly TRange<FFrameNumber> Range;
    public readonly FMovieSceneSegmentIdentifier ID;
    public readonly bool bAllowEmpty;
    public readonly FStructFallback[] Impls;

    public FMovieSceneSegment(FAssetArchive Ar)
    {
        if (FSequencerObjectVersion.Get(Ar) < FSequencerObjectVersion.Type.FloatToIntConversion)
        {
            var OldRange = Ar.Read<TRange<float>>();
            Range = new(new(OldRange.LowerBound.Type, OldRange.LowerBound.Value), new(OldRange.UpperBound.Type, OldRange.UpperBound.Value));
        }
        else
        {
            Range = Ar.Read<TRange<FFrameNumber>>();
        }

        if (FSequencerObjectVersion.Get(Ar) > FSequencerObjectVersion.Type.EvaluationTree)
        {
            ID = Ar.Read<FMovieSceneSegmentIdentifier>();
            bAllowEmpty = Ar.ReadBoolean();
        }

        Impls = new FStructFallback[Ar.Read<int>()];
        for (var i = 0; i < Impls.Length; i++)
        {
            Impls[i] = new FStructFallback(Ar, "SectionEvaluationData");
        }
    }
}

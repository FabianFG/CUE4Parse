using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public readonly struct FMovieSceneSegment : IUStruct
    {
        public readonly TRange<FFrameNumber> Range;
        public readonly FMovieSceneSegmentIdentifier ID;
        public readonly bool bAllowEmpty;
        public readonly FSectionEvaluationData[] Impls;

        public FMovieSceneSegment(FArchive Ar)
        {
            Range = Ar.Read<TRange<FFrameNumber>>();
            ID = Ar.Read<FMovieSceneSegmentIdentifier>();
            bAllowEmpty = Ar.ReadBoolean();
            Impls = Ar.ReadArray<FSectionEvaluationData>();
        }
    }
}

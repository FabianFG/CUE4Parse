using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation
{
    public class FMovieSceneEvaluationFieldEntityTree : IUStruct
    {
        public TMovieSceneEvaluationTree<FEntityAndMetaDataIndex> SerializedData;

        public FMovieSceneEvaluationFieldEntityTree(FArchive Ar)
        {
            SerializedData = new TMovieSceneEvaluationTree<FEntityAndMetaDataIndex>(Ar);
        }

        public struct FEntityAndMetaDataIndex
        {
            public int EntityIndex;
            public int MetaDataIndex;

            public bool Equals(FEntityAndMetaDataIndex other) => EntityIndex == other.EntityIndex && MetaDataIndex == other.MetaDataIndex;
            public override bool Equals(object? obj) => obj is FEntityAndMetaDataIndex other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(EntityIndex, MetaDataIndex);
        }
    }
}
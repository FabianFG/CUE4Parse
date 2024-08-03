using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FMovieSceneSubSequenceTree(FAssetArchive Ar) : IUStruct
{
    public readonly TMovieSceneEvaluationTree<FMovieSceneSubSequenceTreeEntry> Data = new(Ar, () => new FMovieSceneSubSequenceTreeEntry(Ar));
}

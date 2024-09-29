using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FMovieSceneTrackFieldData(FAssetArchive Ar) : IUStruct
{
    public readonly TMovieSceneEvaluationTree<uint> Field = new(Ar, Ar.Read<uint>);
}

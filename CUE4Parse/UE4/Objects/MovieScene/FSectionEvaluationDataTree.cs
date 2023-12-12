using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FSectionEvaluationDataTree : IUStruct, ISerializable
{
    public readonly TMovieSceneEvaluationTree<FSectionEvaluationData> Tree;

    public FSectionEvaluationDataTree(FArchive Ar)
    {
        Tree = new TMovieSceneEvaluationTree<FSectionEvaluationData>(Ar);
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Tree);
    }
}
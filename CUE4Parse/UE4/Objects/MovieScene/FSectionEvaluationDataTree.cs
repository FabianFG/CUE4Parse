using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FSectionEvaluationDataTree : IUStruct
{
    public readonly TMovieSceneEvaluationTree<FSectionEvaluationData> Tree;

    public FSectionEvaluationDataTree(FArchive Ar)
    {
        Tree = new TMovieSceneEvaluationTree<FSectionEvaluationData>(Ar, () => new FSectionEvaluationData(new FStructFallback((FAssetArchive)Ar, "SectionEvaluationData")));
    }
}

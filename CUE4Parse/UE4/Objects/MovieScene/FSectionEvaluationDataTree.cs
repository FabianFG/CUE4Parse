using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public readonly struct FSectionEvaluationDataTree : IUStruct
    {
        public readonly TMovieSceneEvaluationTree<FSectionEvaluationData> Tree;

        public FSectionEvaluationDataTree(FAssetArchive Ar)
        {
            Tree = new TMovieSceneEvaluationTree<FSectionEvaluationData>(Ar);
        }
    }
}

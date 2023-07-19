using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public class FMovieSceneEvaluationTree : IUStruct
    {
        /** This tree's root node */
        public readonly FMovieSceneEvaluationTreeNode RootNode;
        /** Segmented array of all child nodes within this tree (in no particular order) */
        public readonly TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode> ChildNodes;

        public FMovieSceneEvaluationTree(FArchive Ar)
        {
            RootNode = Ar.Read<FMovieSceneEvaluationTreeNode>();
            ChildNodes = new TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode>(Ar);
        }
    }
}

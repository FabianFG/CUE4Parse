using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneEvaluationTreeNodeHandle : IUStruct
    {
        /** Entry handle for the parent's children in FMovieSceneEvaluationTree::ChildNodes */
        public readonly FEvaluationTreeEntryHandle ChildrenHandle;
        /** The index of this child within its parent's children */
        public readonly int Index;
    }
}

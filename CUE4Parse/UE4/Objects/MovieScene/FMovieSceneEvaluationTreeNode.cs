using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneEvaluationTreeNode : IUStruct
    {
        /** The time-range that this node represents */
        public readonly TRange<FFrameNumber> Range;
        /** Handle to the parent node */
        public readonly FMovieSceneEvaluationTreeNodeHandle Parent;
        /** Identifier for the child node entries associated with this node (FMovieSceneEvaluationTree::ChildNodes) */
        public readonly FEvaluationTreeEntryHandle ChildrenID;
        /** Identifier for externally stored data entries associated with this node */
        public readonly FEvaluationTreeEntryHandle DataID;
    }
}

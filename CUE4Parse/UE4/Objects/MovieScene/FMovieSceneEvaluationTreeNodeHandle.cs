using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.MovieScene;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FMovieSceneEvaluationTreeNodeHandle : IUStruct, ISerializable
{
    /** Entry handle for the parent's children in FMovieSceneEvaluationTree::ChildNodes */
    public readonly FEvaluationTreeEntryHandle ChildrenHandle;
    /** The index of this child within its parent's children */
    public readonly int Index;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(ChildrenHandle);
        Ar.Write(Index);
    }
}
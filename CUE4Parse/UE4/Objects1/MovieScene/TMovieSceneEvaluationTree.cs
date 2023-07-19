using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public class TMovieSceneEvaluationTree<T> : FMovieSceneEvaluationTree where T : struct
    {
        /** Tree data container that corresponds to FMovieSceneEvaluationTreeNode::DataID */
        public readonly TEvaluationTreeEntryContainer<T> Data;

        public TMovieSceneEvaluationTree(FArchive Ar) : base(Ar)
        {
            Data = new TEvaluationTreeEntryContainer<T>(Ar);
        }
    }

    public readonly struct TEvaluationTreeEntryContainer<T> : IUStruct where T : struct
    {
        /** List of allocated entries for each allocated entry. Should only ever grow, never shrink. Shrinking would cause previously established handles to become invalid. */
        public readonly FEntry[] Entries;
        /** Linear array of allocated entry contents. Once allocated, indices are never invalidated until Compact is called. Entries needing more capacity are re-allocated on the end of the array. */
        public readonly T[] Items;

        public TEvaluationTreeEntryContainer(FArchive Ar)
        {
            Entries = Ar.ReadArray<FEntry>();
            Items = Ar.ReadArray<T>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FEntry : IUStruct
    {
        /** The index into Items of the first item */
        public readonly int StartIndex;
        /** The number of currently valid items */
        public readonly int Size;
        /** The total capacity of allowed items before reallocating */
        public readonly int Capacity;
    }
}

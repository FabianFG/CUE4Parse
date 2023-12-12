using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.MovieScene;

public class TMovieSceneEvaluationTree<T> : FMovieSceneEvaluationTree, ISerializable where T : struct, ISerializable
{
    /** Tree data container that corresponds to FMovieSceneEvaluationTreeNode::DataID */
    public readonly TEvaluationTreeEntryContainer<T> Data;

    public TMovieSceneEvaluationTree(FArchive Ar) : base(Ar)
    {
        Data = new TEvaluationTreeEntryContainer<T>(Ar);
    }

    public new void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        Ar.Serialize(Data);
    }
}

public readonly struct TEvaluationTreeEntryContainer<T> : ISerializable, IUStruct where T : struct, ISerializable
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

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.SerializeEnumerable(Entries);
        Ar.SerializeEnumerable(Items);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FEntry : IUStruct, ISerializable
{
    /** The index into Items of the first item */
    public readonly int StartIndex;
    /** The number of currently valid items */
    public readonly int Size;
    /** The total capacity of allowed items before reallocating */
    public readonly int Capacity;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(StartIndex);
        Ar.Write(Size);
        Ar.Write(Capacity);
    }
}
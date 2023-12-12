using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.MovieScene;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FEvaluationTreeEntryHandle : IUStruct, ISerializable
{
    /** Specifies an index into TEvaluationTreeEntryContainer<T>::Entries */
    public readonly int EntryIndex;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(EntryIndex);
    }
}
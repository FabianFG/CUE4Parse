using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FEvaluationTreeEntryHandle : IUStruct
    {
        /** Specifies an index into TEvaluationTreeEntryContainer<T>::Entries */
        public readonly int EntryIndex;
    }
}

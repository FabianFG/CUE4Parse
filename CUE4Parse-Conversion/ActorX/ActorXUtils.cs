using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.ActorX;

public static class ActorXUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SerializeChunkHeader(this FArchiveWriter Ar, VChunkHeader header, string name)
    {
        header.ChunkId = name;
        header.Serialize(Ar);
    }
}
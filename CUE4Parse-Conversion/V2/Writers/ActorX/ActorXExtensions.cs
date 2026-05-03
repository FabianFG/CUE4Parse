using System;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public static class ActorXExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SerializeChunkHeader(this FArchiveWriter Ar, VChunkHeader header, string name)
    {
        header.ChunkId = name;
        header.Serialize(Ar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this FArchiveWriter Ar, string value, int len)
    {
        var padded = new byte[len];
        var bytes = Encoding.UTF8.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, padded, 0, Math.Min(bytes.Length, padded.Length));
        Ar.Write(padded);
    }
}

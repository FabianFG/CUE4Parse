using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public readonly struct FIoChunkHash
{
    public readonly Bytes32 Hash;

    public FIoChunkHash(FArchive Ar)
    {
        Hash = default;
        Ar.ReadExactly(Hash);
    }

    [InlineArray(32)]
    public struct Bytes32
    {
        private byte Hash;
    }
}


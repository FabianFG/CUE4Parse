using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public readonly struct FIoChunkHash(FArchive Ar)
{
    public readonly byte[] Hash = Ar.ReadBytes(32);
}

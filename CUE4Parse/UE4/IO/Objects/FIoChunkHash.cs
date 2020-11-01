using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public readonly struct FIoChunkHash
    {
        public readonly byte[] Hash;

        public FIoChunkHash(FArchive Ar)
        {
            Hash = Ar.ReadBytes(32);
        }
    }
}
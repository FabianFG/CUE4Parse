using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Objects
{
    public readonly struct FCompressedChunk
    {
        public readonly int UncompressedOffset;
        public readonly int UncompressedSize;
        public readonly int CompressedOffset;
        public readonly int CompressedSize;

        // currently this is useless but it'll be used for future custom game changes
        public FCompressedChunk(FArchive Ar)
        {
            UncompressedOffset = Ar.Read<int>();
            UncompressedSize = Ar.Read<int>();
            CompressedOffset = Ar.Read<int>();
            CompressedSize = Ar.Read<int>();
        }
    }
}
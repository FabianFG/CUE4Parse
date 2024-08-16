using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EOnDemandTocVersion : uint
    {
        Invalid			= 0,
        Initial			= 1,
        UTocHash		= 2,
        BlockHash32		= 3,
        NoRawHash		= 4,
        Meta			= 5,
        ContainerId		= 6,
        AdditionalFiles	= 7,
        TagSets			= 8,
        ContainerFlags	= 9,

        LatestPlusOne,
        Latest			= (LatestPlusOne - 1)
    }

    public enum EOnDemandChunkVersion : uint
    {
        Invalid			= 0,
        Initial			= 1,

        LatestPlusOne,
        Latest			= (LatestPlusOne - 1)
    }

    public class FOnDemandTocHeader
    {
        public const ulong ExpectedMagic = 0x6f6e64656d616e64; // ondemand

        public readonly ulong Magic;
        public readonly EOnDemandTocVersion Version;
        public readonly EOnDemandChunkVersion ChunkVersion;
        public readonly uint BlockSize;
        public readonly string CompressionFormat;
        public readonly string ChunksDirectory;

        public FOnDemandTocHeader(FArchive Ar)
        {
            Magic = Ar.Read<ulong>();
            if (Magic != ExpectedMagic)
                throw new ParserException(Ar, "Invalid iochunktoc magic");
            Version = Ar.Read<EOnDemandTocVersion>();
            if (Version == EOnDemandTocVersion.Invalid)
                throw new ParserException(Ar, "Invalid iochunktoc version");

            ChunkVersion = Ar.Read<EOnDemandChunkVersion>();
            BlockSize = Ar.Read<uint>();
            CompressionFormat = Ar.ReadFString();
            ChunksDirectory = Ar.ReadFString();
        }
    }
}

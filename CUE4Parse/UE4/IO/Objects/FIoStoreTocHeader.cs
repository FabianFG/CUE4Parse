using System.Linq;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EIoStoreTocVersion : byte
    {
        Invalid = 0,
        Initial,
        DirectoryIndex,
        PartitionSize,
        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public enum EIoContainerFlags : byte
    {
        None,
        Compressed	= (1 << 0),
        Encrypted	= (1 << 1),
        Signed		= (1 << 2),
        Indexed		= (1 << 3),
    }

    public class FIoStoreTocHeader
    {
        public const int SIZE = 144;
        public static byte[] TOC_MAGIC = new byte[]
            {0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D}; // -==--==--==--==-

        public readonly byte[] TocMagic;
        public readonly EIoStoreTocVersion Version;
        private readonly byte _reserved0;
        private readonly ushort _reserved1;
        public readonly uint TocHeaderSize;
        public readonly uint TocEntryCount;
        public readonly uint TocCompressedBlockEntryCount;
        public readonly uint TocCompressedBlockEntrySize;	// For sanity checking
        public readonly uint CompressionMethodNameCount;
        public readonly uint CompressionMethodNameLength;
        public readonly uint CompressionBlockSize;
        public readonly uint DirectoryIndexSize;
        public uint PartitionCount;
        public readonly FIoContainerId ContainerId;
        public readonly FGuid EncryptionKeyGuid;
        public readonly EIoContainerFlags ContainerFlags;
        private readonly byte _reserved3;
        private readonly ushort _reserved4;
        private readonly uint _reserved5;
        public ulong PartitionSize;
        private readonly ulong[] _reserved6;

        public FIoStoreTocHeader(FArchive Ar)
        {
            TocMagic = Ar.ReadBytes(16);
            if (!TOC_MAGIC.SequenceEqual(TocMagic))
                throw new ParserException(Ar, "Invalid utoc magic");
            Version = Ar.Read<EIoStoreTocVersion>();
            _reserved0 = Ar.Read<byte>();
            _reserved1 = Ar.Read<ushort>();
            TocHeaderSize = Ar.Read<uint>();
            TocEntryCount = Ar.Read<uint>();
            TocCompressedBlockEntryCount = Ar.Read<uint>();
            TocCompressedBlockEntrySize = Ar.Read<uint>();
            CompressionMethodNameCount = Ar.Read<uint>();
            CompressionMethodNameLength = Ar.Read<uint>();
            CompressionBlockSize = Ar.Read<uint>();
            DirectoryIndexSize = Ar.Read<uint>();
            PartitionCount = Ar.Read<uint>();
            ContainerId = Ar.Read<FIoContainerId>();
            EncryptionKeyGuid = Ar.Read<FGuid>();
            ContainerFlags = Ar.Read<EIoContainerFlags>();
            _reserved3 = Ar.Read<byte>();
            _reserved4 = Ar.Read<ushort>();
            _reserved5 = Ar.Read<uint>();
            PartitionSize = Ar.Read<ulong>();
            _reserved6 = Ar.ReadArray<ulong>(6);
        }
    }
}
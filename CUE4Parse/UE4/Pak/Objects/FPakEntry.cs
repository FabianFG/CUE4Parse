using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Vfs;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak.Objects
{
    public class FPakEntry : VfsEntry
    {
        public readonly long CompressedSize;
        public readonly long UncompressedSize;
        public override CompressionMethod CompressionMethod { get; }
        public readonly FPakCompressedBlock[] CompressionBlocks = new FPakCompressedBlock[0];
        public override bool IsEncrypted { get; }
        public readonly uint CompressionBlockSize;

        public readonly int StructSize;    // computed value: size of FPakEntry prepended to each file
        public bool IsCompressed => UncompressedSize != CompressedSize || CompressionMethod != CompressionMethod.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FPakEntry(PakFileReader reader, string path, FArchive Ar) : base(reader)
        {
            Path = path;
            // FPakEntry is duplicated before each stored file, without a filename. So,
            // remember the serialized size of this structure to avoid recomputation later.
            var startOffset = Ar.Position;
            Offset = Ar.Read<long>();
            CompressedSize = Ar.Read<long>();
            UncompressedSize = Ar.Read<long>();
            Size = UncompressedSize;

            if (reader.Info.Version < EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
            {
                var LegacyCompressionMethod = Ar.Read<ECompressionFlags>();
                int CompressionMethodIndex;

                if (LegacyCompressionMethod == ECompressionFlags.COMPRESS_None)
                {
                    CompressionMethodIndex = 0;
                }
                else if (LegacyCompressionMethod == ECompressionFlags.COMPRESS_LZ4)
                {
                    CompressionMethodIndex = 4;
                }
                else if (LegacyCompressionMethod.HasFlag(ECompressionFlags.COMPRESS_ZLIB))
                {
                    CompressionMethodIndex = 1;
                }
                else if (LegacyCompressionMethod.HasFlag(ECompressionFlags.COMPRESS_GZIP))
                {
                    CompressionMethodIndex = 2;
                }
                else if (LegacyCompressionMethod.HasFlag(ECompressionFlags.COMPRESS_Custom))
                {
                    CompressionMethodIndex = 3;
                }
                else
                {
                    CompressionMethodIndex = -1;
                    //throw new ParserException("Found an unknown compression type in pak file, will need to be supported for legacy files");
                }

                CompressionMethod = CompressionMethodIndex == -1 ? CompressionMethod.Unknown : reader.Info.CompressionMethods[CompressionMethodIndex];
            }
            else if (reader.Info.Version == EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod && !reader.Info.IsSubVersion)
            {
                CompressionMethod = reader.Info.CompressionMethods[Ar.Read<byte>()];
            }
            else
            {
                CompressionMethod = reader.Info.CompressionMethods[Ar.Read<int>()];
            }

            if (reader.Info.Version < EPakFileVersion.PakFile_Version_NoTimestamps)
                Ar.Position += 8; // Timestamp
            Ar.Position += 20; // Hash
            if (reader.Info.Version >= EPakFileVersion.PakFile_Version_CompressionEncryption)
            {
                if (CompressionMethod != CompressionMethod.None)
                    CompressionBlocks = Ar.ReadArray<FPakCompressedBlock>();
                IsEncrypted = Ar.ReadFlag();
                CompressionBlockSize = Ar.Read<uint>();
            }

            if (reader.Info.Version >= EPakFileVersion.PakFile_Version_RelativeChunkOffsets)
            {
                // Convert relative compressed offsets to absolute
                for (var i = 0; i < CompressionBlocks.Length; i++)
                {
                    CompressionBlocks[i].CompressedStart += Offset;
                    CompressionBlocks[i].CompressedEnd += Offset;
                }
            }

            StructSize = (int)(Ar.Position - startOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe FPakEntry(PakFileReader reader, string path, byte* data) : base(reader)
        {
            Path = path;
            Ver = reader.Ar.Ver;
            Game = reader.Ar.Game;

            // UE4 reference: FPakFile::DecodePakEntry()
            uint bitfield = *(uint*) data;
            data += sizeof(uint);

            CompressionBlockSize = 0;
            if ((bitfield & 0x3f) == 0x3f) // flag value to load a field
            {
                CompressionBlockSize = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                // for backwards compatibility with old paks :
                CompressionBlockSize = (bitfield & 0x3f) << 11;
            }

            CompressionMethod = reader.Info.CompressionMethods[(int)((bitfield >> 23) & 0x3f)];

            // Offset follows - either 32 or 64 bit value
            if ((bitfield & 0x80000000) != 0)
            {
                Offset = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                Offset = *(long*) data; // Should be ulong
                data += sizeof(long);
            }

            // The same for UncompressedSize
            if ((bitfield & 0x40000000) != 0)
            {
                UncompressedSize = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                UncompressedSize = *(long*) data; // Should be ulong
                data += sizeof(long);
            }

            Size = UncompressedSize;

            // Size field
            if (CompressionMethod != CompressionMethod.None)
            {
                if ((bitfield & 0x20000000) != 0)
                {
                    CompressedSize = *(uint*) data;
                    data += sizeof(uint);
                }
                else
                {
                    CompressedSize = *(long*) data;
                    data += sizeof(long);
                }
            }
            else
            {
                CompressedSize = UncompressedSize;
            }

            // bEncrypted
            IsEncrypted = ((bitfield >> 22) & 1) != 0;

            // Compressed block count
            var blockCount = (bitfield >> 6) & 0xffff;

            // Compute StructSize: each file still have FPakEntry data prepended, and it should be skipped.
            StructSize = sizeof(long) * 3 + sizeof(int) * 2 + 1 + 20;
            // Take into account CompressionBlocks
            if (CompressionMethod != CompressionMethod.None)
                StructSize += (ushort) (sizeof(int) + blockCount * 2 * sizeof(long));

            // Compression information
            CompressionBlocks = new FPakCompressedBlock[blockCount];
            if (blockCount > 0)
            {
                // CompressionBlockSize
                if (UncompressedSize < 65536)
                    CompressionBlockSize = (uint) UncompressedSize;

                // CompressionBlocks
                if (blockCount == 1 && !IsEncrypted)
                {
                    ref var b = ref CompressionBlocks[0];
                    b.CompressedStart = Offset + StructSize;
                    b.CompressedEnd = b.CompressedStart + CompressedSize;
                }
                else
                {
                    var currentOffset = Offset + StructSize;
                    var alignment = IsEncrypted ? Aes.ALIGN : 1;

                    for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                    {
                        var currentBlockSize = *(uint*) data;
                        data += sizeof(uint);

                        ref var block = ref CompressionBlocks[blockIndex];
                        block.CompressedStart = currentOffset;
                        block.CompressedEnd = currentOffset + currentBlockSize;
                        currentOffset += currentBlockSize.Align(alignment);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read() => Vfs.Extract(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Game, Ver);
    }
}
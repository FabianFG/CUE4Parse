using System;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using static CUE4Parse.UE4.Objects.Core.Misc.ECompressionFlags;
using static CUE4Parse.UE4.Pak.Objects.EPakFileVersion;
using static CUE4Parse.UE4.Versions.EGame;

namespace CUE4Parse.UE4.Pak.Objects
{
    public class FPakEntry : VfsEntry
    {
        private const byte Flag_None = 0x00;
        private const byte Flag_Encrypted = 0x01;
        private const byte Flag_Deleted = 0x02;

        public readonly long CompressedSize;
        public readonly long UncompressedSize;
        public sealed override CompressionMethod CompressionMethod { get; }
        public readonly FPakCompressedBlock[] CompressionBlocks = Array.Empty<FPakCompressedBlock>();
        public readonly uint Flags;
        public override bool IsEncrypted => (Flags & Flag_Encrypted) == Flag_Encrypted;
        public bool IsDeleted => (Flags & Flag_Deleted) == Flag_Deleted;
        public readonly uint CompressionBlockSize;

        public readonly int StructSize; // computed value: size of FPakEntry prepended to each file
        public bool IsCompressed => UncompressedSize != CompressedSize && CompressionBlockSize > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FPakEntry(PakFileReader reader, string path, FArchive Ar) : base(reader)
        {
            Path = path;
            // FPakEntry is duplicated before each stored file, without a filename. So,
            // remember the serialized size of this structure to avoid recomputation later.
            var startOffset = Ar.Position;

            Offset = Ar.Read<long>();

            if (Ar.Game == GAME_GearsOfWar4)
            {
                CompressedSize = Ar.Read<int>();
                UncompressedSize = Ar.Read<int>();
                CompressionMethod = (CompressionMethod) Ar.Read<byte>();

                if (reader.Info.Version < PakFile_Version_NoTimestamps)
                {
                    Ar.Position += 8;
                }

                if (reader.Info.Version >= PakFile_Version_CompressionEncryption)
                {
                    if (CompressionMethod != CompressionMethod.None)
                        CompressionBlocks = Ar.ReadArray<FPakCompressedBlock>();
                    CompressionBlockSize = Ar.Read<uint>();
                    if (CompressionMethod == CompressionMethod.Oodle)
                        CompressionMethod = CompressionMethod.LZ4;
                }

                goto endRead;
            }

            CompressedSize = Ar.Read<long>();
            UncompressedSize = Ar.Read<long>();
            Size = UncompressedSize;

            if (reader.Info.Version < PakFile_Version_FNameBasedCompressionMethod)
            {
                var legacyCompressionMethod = Ar.Read<ECompressionFlags>();
                int compressionMethodIndex;

                if (legacyCompressionMethod == COMPRESS_None)
                {
                    compressionMethodIndex = 0;
                }
                else if (legacyCompressionMethod == (ECompressionFlags) 259) // SOD2
                {
                    compressionMethodIndex = 4;
                }
                else if (legacyCompressionMethod.HasFlag(COMPRESS_ZLIB))
                {
                    compressionMethodIndex = 1;
                }
                else if (legacyCompressionMethod.HasFlag(COMPRESS_GZIP))
                {
                    compressionMethodIndex = 2;
                }
                else if (legacyCompressionMethod.HasFlag(COMPRESS_Custom))
                {
                    if (reader.Game == GAME_SeaOfThieves)
                    {
                        compressionMethodIndex = 4; // LZ4
                    }
                    else
                    {
                        compressionMethodIndex = 3; // Oodle, used by Fortnite Mobile until early 2019
                    }
                }
                else
                {
                    if (reader.Game == GAME_PlayerUnknownsBattlegrounds)
                    {
                        compressionMethodIndex = 3; // TODO: Investigate what a proper detection is.
                    }
                    else if (reader.Game == GAME_DeadIsland2)
                    {
                        compressionMethodIndex = 6; // ¯\_(ツ)_/¯
                    }
                    else
                    {
                        compressionMethodIndex = -1;
                        // throw new ParserException("Found an unknown compression type in pak file, will need to be supported for legacy files");
                    }
                }

                CompressionMethod = compressionMethodIndex == -1 ? CompressionMethod.Unknown : reader.Info.CompressionMethods[compressionMethodIndex];
            }
            else if (reader.Info.Version == PakFile_Version_FNameBasedCompressionMethod && !reader.Info.IsSubVersion)
            {
                CompressionMethod = reader.Info.CompressionMethods[Ar.Read<byte>()];
            }
            else
            {
                CompressionMethod = reader.Info.CompressionMethods[Ar.Read<int>()];
            }

            if (reader.Info.Version < PakFile_Version_NoTimestamps)
                Ar.Position += 8; // Timestamp
            Ar.Position += 20; // Hash
            if (reader.Info.Version >= PakFile_Version_CompressionEncryption)
            {
                if (CompressionMethod != CompressionMethod.None)
                    CompressionBlocks = Ar.ReadArray<FPakCompressedBlock>();
                Flags = (uint)Ar.ReadByte();
                CompressionBlockSize = Ar.Read<uint>();
            }

            if (Ar.Game == GAME_TEKKEN7) Flags = (uint) (Flags & ~Flag_Encrypted);

            if (reader.Info.Version >= PakFile_Version_RelativeChunkOffsets)
            {
                // Convert relative compressed offsets to absolute
                for (var i = 0; i < CompressionBlocks.Length; i++)
                {
                    CompressionBlocks[i].CompressedStart += Offset;
                    CompressionBlocks[i].CompressedEnd += Offset;
                }
            }

            endRead:
            StructSize = (int) (Ar.Position - startOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe FPakEntry(PakFileReader reader, string path, byte* data) : base(reader)
        {
            Path = path;

            // UE4 reference: FPakFile::DecodePakEntry()
            uint bitfield = *(uint*) data;
            data += sizeof(uint);

            uint compressionBlockSize;
            if ((bitfield & 0x3f) == 0x3f) // flag value to load a field
            {
                compressionBlockSize = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                // for backwards compatibility with old paks :
                compressionBlockSize = (bitfield & 0x3f) << 11;
            }

            // Filter out the CompressionMethod.
            CompressionMethod = reader.Info.CompressionMethods[(int) ((bitfield >> 23) & 0x3f)];

            // Test for 32-bit safe values. Grab it, or memcpy the 64-bit value
            // to avoid alignment exceptions on platforms requiring 64-bit alignment
            // for 64-bit variables.
            //
            // Read the Offset.
            var bIsOffset32BitSafe = (bitfield & (1 << 31)) != 0;
            if (bIsOffset32BitSafe)
            {
                Offset = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                Offset = *(long*) data; // Should be ulong
                data += sizeof(long);
            }

            if (reader.Ar.Game == GAME_Snowbreak) Offset ^= 0x1F1E1D1C;

            // Read the UncompressedSize.
            var bIsUncompressedSize32BitSafe = (bitfield & (1 << 30)) != 0;
            if (bIsUncompressedSize32BitSafe)
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

            // Fill in the Size.
            if (CompressionMethod != CompressionMethod.None)
            {
                var bIsSize32BitSafe = (bitfield & (1 << 29)) != 0;
                if (bIsSize32BitSafe)
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
                // The Size is the same thing as the UncompressedSize when
                // CompressionMethod == CompressionMethod.None.
                CompressedSize = UncompressedSize;
            }

            // Filter the encrypted flag.
            Flags |= (bitfield & (1 << 22)) != 0 ? 1u : 0u;

            // This should clear out any excess CompressionBlocks that may be valid in the user's
            // passed in entry.
            var compressionBlocksCount = (bitfield >> 6) & 0xffff;
            CompressionBlocks = new FPakCompressedBlock[compressionBlocksCount];

            CompressionBlockSize = 0;
            if (compressionBlocksCount > 0)
            {
                CompressionBlockSize = compressionBlockSize;
                // Per the comment in Encode, if compressionBlocksCount == 1, we use UncompressedSize for CompressionBlockSize
                if (compressionBlocksCount == 1)
                {
                    CompressionBlockSize = (uint) UncompressedSize;
                }
            }

            // Compute StructSize: each file still have FPakEntry data prepended, and it should be skipped.
            StructSize = sizeof(long) * 3 + sizeof(int) * 2 + 1 + 20;
            // Take into account CompressionBlocks
            if (CompressionMethod != CompressionMethod.None)
                StructSize += (int) (sizeof(int) + compressionBlocksCount * 2 * sizeof(long));

            // Handle building of the CompressionBlocks array.
            if (compressionBlocksCount == 1 && !IsEncrypted)
            {
                // If the number of CompressionBlocks is 1, we didn't store any extra information.
                // Derive what we can from the entry's file offset and size.
                ref var b = ref CompressionBlocks[0];
                b.CompressedStart = Offset + StructSize;
                b.CompressedEnd = b.CompressedStart + CompressedSize;
            }
            else if (compressionBlocksCount > 0)
            {
                // Get the right pointer to start copying the CompressionBlocks information from.
                var compressionBlockSizePtr = (uint*) data;

                // Alignment of the compressed blocks
                var compressedBlockAlignment = IsEncrypted ? Aes.ALIGN : 1;

                // compressedBlockOffset is the starting offset. Everything else can be derived from there.
                var compressedBlockOffset = Offset + StructSize;
                for (int compressionBlockIndex = 0; compressionBlockIndex < compressionBlocksCount; ++compressionBlockIndex)
                {
                    ref var compressedBlock = ref CompressionBlocks[compressionBlockIndex];
                    compressedBlock.CompressedStart = compressedBlockOffset;
                    compressedBlock.CompressedEnd = compressedBlockOffset + *compressionBlockSizePtr++;
                    compressedBlockOffset += (compressedBlock.CompressedEnd - compressedBlock.CompressedStart).Align(compressedBlockAlignment);
                }
            }
        }

        public FPakEntry(PakFileReader reader, FMemoryImageArchive Ar) : base(reader)
        {
            Offset = Ar.Read<long>();
            CompressedSize = Ar.Read<long>();
            UncompressedSize = Ar.Read<long>();
            Size = UncompressedSize;
            Ar.Position += FSHAHash.SIZE + 4 /*align to 8 bytes*/; //Hash = new FSHAHash(Ar);
            CompressionBlocks = Ar.ReadArray<FPakCompressedBlock>();
            CompressionBlockSize = Ar.Read<uint>();
            CompressionMethod = reader.Info.CompressionMethods[Ar.Read<int>()];
            Flags = Ar.Read<byte>();

            if (reader.Info.Version >= PakFile_Version_RelativeChunkOffsets)
            {
                // Convert relative compressed offsets to absolute
                for (var i = 0; i < CompressionBlocks.Length; i++)
                {
                    CompressionBlocks[i].CompressedStart += Offset;
                    CompressionBlocks[i].CompressedEnd += Offset;
                }
            }

            // Compute StructSize: each file still have FPakEntry data prepended, and it should be skipped.
            StructSize = sizeof(long) * 3 + sizeof(int) * 2 + 1 + 20;
            // Take into account CompressionBlocks
            if (CompressionMethod != CompressionMethod.None)
                StructSize += (int) (sizeof(int) + CompressionBlocks.Length * 2 * sizeof(long));
        }

        public PakFileReader PakFileReader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (PakFileReader) Vfs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read() => Vfs.Extract(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Vfs.Versions);
    }
}

using System;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak.Objects
{
    public class FPakEntry
    {
        public readonly long Pos;
        public readonly long Size;
        public readonly long UncompressedSize;
        public readonly CompressionMethod CompressionMethod;
        public readonly FPakCompressedBlock[] CompressionBlocks;
        public readonly bool IsEncrypted;
        public readonly int CompressionBlockSize;

        public readonly ushort StructSize;    // computed value: size of FPakEntry prepended to each file

        public FPakEntry(FArchive Ar, FPakInfo info)
        {
            // FPakEntry is duplicated before each stored file, without a filename. So,
            // remember the serialized size of this structure to avoid recomputation later.
            var startOffset = Ar.Position;
            Pos = Ar.Read<long>();
            Size = Ar.Read<long>();
            UncompressedSize = Ar.Read<long>();
            
            if (info.Version >= EPakFileVersion.PakFile_Version_FNameBasedCompressionMethod)
            {
                try
                {
                    CompressionMethod = info.CompressionMethods[Ar.Read<int>()];
                }
                catch (Exception e)
                {
                    CompressionMethod = CompressionMethod.Unknown;
                }
            }
            else
            {
                CompressionMethod = (CompressionMethod) Ar.Read<int>();
            }

            if (info.Version < EPakFileVersion.PakFile_Version_NoTimestamps)
                Ar.Position += 8; // Timestamp
            Ar.Position += 20; // Hash
            if (info.Version >= EPakFileVersion.PakFile_Version_CompressionEncryption)
            {
                if (CompressionMethod != CompressionMethod.None)
                    CompressionBlocks = Ar.ReadArray<FPakCompressedBlock>();
                IsEncrypted = Ar.ReadFlag();
                CompressionBlockSize = Ar.Read<int>();
            }

            if (info.Version >= EPakFileVersion.PakFile_Version_RelativeChunkOffsets)
            {
                // Convert relative compressed offsets to absolute
                for (int i = 0; i < CompressionBlocks.Length; i++)
                {
                    CompressionBlocks[i].CompressedStart += Pos;
                    CompressionBlocks[i].CompressedEnd += Pos;
                }
            }

            StructSize = (ushort) (Ar.Position - startOffset);
        }

        public unsafe FPakEntry(byte* data)
        {
            // UE4 reference: FPakFile::DecodePakEntry()
            uint bitfield = *(uint*) data;
            data += sizeof(uint);

            CompressionMethod = (CompressionMethod) ((bitfield >> 23) & 0x3f);
            
            // Offset follows - either 32 or 64 bit value
            if ((bitfield & 0x80000000) != 0)
            {
                Pos = *(uint*) data;
                data += sizeof(uint);
            }
            else
            {
                Pos = *(long*) data; // Should be ulong
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
            
            // Size field
            if (CompressionMethod != CompressionMethod.None)
            {
                if ((bitfield & 0x20000000) != 0)
                {
                    Size = *(uint*) data;
                    data += sizeof(uint);
                }
                else
                {
                    Size = *(long*) data;
                    data += sizeof(long);
                }
            }
            else
            {
                Size = UncompressedSize;
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
            CompressionBlockSize = 0;
            if (blockCount != 0)
            {
                // CompressionBlockSize
                if (UncompressedSize < 65536)
                    CompressionBlockSize = (int) UncompressedSize;
                else
                    CompressionBlockSize = (int) ((bitfield & 0x3f) << 11);
                
                // CompressionBlocks
                if (blockCount == 1)
                {
                    ref var b = ref CompressionBlocks[0];
                    b.CompressedStart = Pos + StructSize;
                    b.CompressedEnd = b.CompressedStart + Size;
                }
                else
                {
                    var currentOffset = Pos + StructSize;
                    var alignment = IsEncrypted ? Aes.ALIGN : 1;
                    for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                    {
                        var currentBlockSize = *(long*) data;
                        data += sizeof(long);

                        ref var block = ref CompressionBlocks[0];
                        block.CompressedStart = currentOffset;
                        block.CompressedEnd = block.CompressedStart + currentBlockSize;
                        currentOffset += currentBlockSize.Align(alignment);
                    }
                }
            }
        }
    }
}
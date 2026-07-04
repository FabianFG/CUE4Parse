using System.Buffers.Binary;
using CUE4Parse.Compression;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.LordOfMysteries;

internal sealed class LoMManifest
{
    private const uint KmfMagic = 0x00464D4B; // KMF\0
    private const int KmfHeaderSize = 24;

    public readonly string BaseDirectory;
    public readonly string[] Names;
    public readonly string[] CompressionMethods;
    public readonly LoMEntry[] Entries;
    public readonly LoMCompressionBlock[] CompressionBlocks;

    private LoMManifest(string baseDirectory, string[] names, string[] compressionMethods, LoMEntry[] entries, LoMCompressionBlock[] compressionBlocks)
    {
        BaseDirectory = baseDirectory;
        Names = names;
        CompressionMethods = compressionMethods;
        Entries = entries;
        CompressionBlocks = compressionBlocks;
    }

    public static LoMManifest Read(FileInfo manifestFile, VersionContainer versions)
    {
        using var Ar = new FByteArchive(manifestFile.FullName, File.ReadAllBytes(manifestFile.FullName), versions);
        using var manifestAr = new FByteArchive(manifestFile.FullName, ReadCompressedData(Ar), versions);

        manifestAr.Read<int>(); // Version
        var names = manifestAr.ReadArray(manifestAr.ReadFString);
        var compressionMethods = manifestAr.ReadArray(manifestAr.ReadFString)
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(nameof(CompressionMethod.None), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var entries = manifestAr.ReadArray(() => new LoMEntry(manifestAr));
        var compressionBlocks = manifestAr.ReadArray(() => new LoMCompressionBlock(manifestAr));
        manifestAr.ReadArray<FIoChunkId>(); // Headers
        manifestAr.ReadArray<int>(); // Chunk IDs

        return new LoMManifest(manifestFile.DirectoryName ?? string.Empty, names, compressionMethods, entries, compressionBlocks);
    }

    private static byte[] ReadCompressedData(FArchive Ar)
    {
        var manifestData = Ar.ReadBytes((int) Ar.Length);
        if (manifestData.Length < KmfHeaderSize)
            return manifestData;

        using var manifestAr = new FByteArchive(Ar.Name, manifestData, Ar.Versions);
        if (manifestAr.Read<uint>() != KmfMagic)
            return manifestData;

        manifestAr.Read<int>(); // Version
        var uncompressedSize = manifestAr.Read<int>();
        manifestAr.Read<int>();
        manifestAr.Read<int>();
        manifestAr.Read<int>();

        var compressedOffset = (int) manifestAr.Position;

        return Compression.Compression.Decompress(manifestData, compressedOffset, manifestData.Length - compressedOffset, uncompressedSize, CompressionMethod.Zlib, Ar);
    }
}

internal record struct LoMEntry
{
    public readonly long Offset;
    public readonly long CompressedSize;
    public readonly long UncompressedSize;
    public readonly ELoMFileType Type;
    public readonly FIoChunkId Id;
    public readonly uint Seed;
    public readonly int Owner;
    public readonly int Const; // 1800314
    public int CompressionBlocksOffset;
    public readonly int CompressionBlocksCount;

    public LoMEntry(FArchive Ar)
    {
        Offset = ReadInt40(Ar);
        CompressedSize = ReadInt40(Ar);
        UncompressedSize = ReadInt40(Ar);
        Type = Ar.Read<ELoMFileType>();
        Id = Ar.Read<FIoChunkId>();
        Seed = Ar.Read<uint>();
        Owner = Ar.Read<int>();
        Const = Ar.Read<int>();
        CompressionBlocksOffset = Ar.Read<int>();
        CompressionBlocksCount = Ar.Read<int>();
    }

    private static long ReadInt40(FArchive Ar)
    {
        Span<byte> buffer = stackalloc byte[8];
        buffer.Clear();
        Ar.ReadSpan(5).CopyTo(buffer);
        return BitConverter.ToInt64(buffer);
    }
}

internal enum ELoMFileType : byte
{
    File,
    UnknownFile,
    Asset = 9,
    Bulk = 11
}

internal readonly struct LoMCompressionBlock
{
    public readonly long Offset;
    public readonly int CompressedSize;
    public readonly int UncompressedSize;
    public readonly byte CompressionMethod;

    public LoMCompressionBlock(FArchive Ar)
    {
        var span = Ar.ReadSpan(FIoStoreTocCompressedBlockEntry.SIZE);
        Offset = (long) (BinaryPrimitives.ReadUInt64LittleEndian(span) & 0xFFFFFFFFFF);
        CompressedSize = (int) (BinaryPrimitives.ReadUInt32LittleEndian(span[5..]) & 0xFFFFFF);
        UncompressedSize = (int) (BinaryPrimitives.ReadUInt32LittleEndian(span[8..]) & 0xFFFFFF);
        CompressionMethod = span[^1];
    }

    public LoMCompressionBlock WithBaseOffset(long baseOffset) => new(Offset + baseOffset, CompressedSize, UncompressedSize, CompressionMethod);

    private LoMCompressionBlock(long offset, int compressedSize, int uncompressedSize, byte compressionMethod)
    {
        Offset = offset;
        CompressedSize = compressedSize;
        UncompressedSize = uncompressedSize;
        CompressionMethod = compressionMethod;
    }
}

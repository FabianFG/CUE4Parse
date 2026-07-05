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
    public readonly string[] Paths;
    public readonly string[] CompressionMethods;
    public readonly LoMEntry[] Entries;
    public readonly LoMCompressionBlock[] CompressionBlocks;

    private LoMManifest(string baseDirectory, string[] paths, string[] compressionMethods, LoMEntry[] entries, LoMCompressionBlock[] compressionBlocks)
    {
        BaseDirectory = baseDirectory;
        Paths = paths;
        CompressionMethods = compressionMethods;
        Entries = entries;
        CompressionBlocks = compressionBlocks;
    }

    public static LoMManifest Read(FileInfo manifestFile, VersionContainer versions)
    {
        var manifestData = File.ReadAllBytes(manifestFile.FullName);
        var decompressedData = ReadCompressedData(manifestFile.FullName, manifestData, versions);
        using var Ar = new FByteArchive(manifestFile.FullName, decompressedData, versions);

        Ar.Read<int>(); // Version
        var paths = Ar.ReadArray(Ar.ReadFString);
        var compressionMethods = Ar.ReadArray(Ar.ReadFString)
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(nameof(CompressionMethod.None), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var entries = Ar.ReadArray(() => new LoMEntry(Ar));
        var compressionBlocks = Ar.ReadArray(() => new LoMCompressionBlock(Ar));
        Ar.ReadArray<FIoChunkId>(); // Headers
        Ar.ReadArray<int>(); // Chunk IDs

        return new LoMManifest(manifestFile.DirectoryName ?? string.Empty, paths, compressionMethods, entries, compressionBlocks);
    }

    private static byte[] ReadCompressedData(string archiveName, byte[] manifestData, VersionContainer versions)
    {
        if (manifestData.Length < KmfHeaderSize)
            return manifestData;

        using var Ar = new FByteArchive(archiveName, manifestData, versions);
        if (Ar.Read<uint>() != KmfMagic)
            return manifestData;

        Ar.Read<int>(); // Version
        var uncompressedSize = Ar.Read<int>();
        Ar.Read<int>();
        Ar.Read<int>();
        Ar.Read<int>();

        var compressedOffset = (int) Ar.Position;

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

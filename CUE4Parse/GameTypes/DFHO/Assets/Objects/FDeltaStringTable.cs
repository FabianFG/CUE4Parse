using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Readers.FArchive;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public class FDeltaStringTable
{
    private const uint ChunkMagic = 0x9E2A83C1;
    private const int ChunkHeaderSize = 32;
    private const int ChunkMetadataStride = 16;
    private const int ChunkSearchWindow = 48;
    private const int LegacySearchWindow = 100;
    private const int MaxUstbinChunkSize = 16 * 1024 * 1024;
    private const string CompressionFormat = "Zlib";

    public string TableNamespace;
    public Dictionary<string, string> KeysToEntries;

    public FDeltaStringTable(FArchive Ar)
    {
        TableNamespace = string.Empty;
        KeysToEntries = [];

        var initialPos = Ar.Position;
        var chunk = Ar.Read<FCompressedChunkInfo>();
        var summary = Ar.Read<FCompressedChunkInfo>();
        Ar.Position = initialPos;

        var uncompressed = DecompressPayload(Ar, initialPos, summary.UncompressedSize);
        var tableAr = new FByteArchive("FDeltaStringTable", uncompressed, Ar.Versions);
        ParseEntries(tableAr, Ar.Name);
    }

    private void ParseEntries(FByteArchive tableAr, string name)
    {
        if (tableAr.Length < sizeof(int))
        {
            return;
        }

        try
        {
            tableAr.Position += 4;
            TableNamespace = tableAr.ReadFString();

            var entryCount = tableAr.Read<int>();

            for (var i = 0; i < entryCount; i++)
            {
                try
                {
                    var key = tableAr.ReadFString();
                    var value = tableAr.ReadFString();
                    KeysToEntries[key] = value;
                }
                catch (Exception e)
                {
                    break;
                }
            }
        }
        catch
        {
        }
    }

    private static byte[] DecompressPayload(FArchive Ar, long initialPos, long summaryUncompressedSize)
    {
        try
        {
            var standard = new byte[summaryUncompressedSize];
            Ar.SerializeCompressedNew(standard, standard.Length, CompressionFormat, ECompressionFlags.COMPRESS_NoFlags, false, out _);
            if (LooksComplete(standard))
            {
                return standard;
            }
        }
        catch
        {
        }

        Ar.Position = initialPos;
        var raw = Ar.ReadBytes((int) (Ar.Length - initialPos));
        var chunked = TryDecompressChunkedUstbin(raw, Ar.Name);
        return chunked.Length > 0 ? chunked : [];
    }

    private static bool LooksComplete(byte[] data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        var declaredSize = BitConverter.ToInt32(data, 0);
        return declaredSize > 0 && declaredSize <= data.Length;
    }

    private static byte[] TryDecompressChunkedUstbin(byte[] raw, string name)
    {
        var blockPositions = FindMagicBlocks(raw);
        if (blockPositions.Count > 0)
        {
            var magicData = TryDecompressMagicBlocks(raw, name, blockPositions);
            if (magicData.Length > 0)
            {
                return magicData;
            }
        }

        return TryDecompressChunkedUstbinLegacy(raw, name);
    }

    private static List<int> FindMagicBlocks(byte[] raw)
    {
        var blockPositions = new List<int>();
        for (var i = 0; i < raw.Length - sizeof(uint); i++)
        {
            if (BitConverter.ToUInt32(raw, i) == ChunkMagic)
            {
                blockPositions.Add(i);
            }
        }

        return blockPositions;
    }

    private static byte[] TryDecompressMagicBlocks(byte[] raw, string name, List<int> blockPositions)
    {
        using var output = new MemoryStream();
        for (var blockIndex = 0; blockIndex < blockPositions.Count; blockIndex++)
        {
            var blockPosition = blockPositions[blockIndex];
            if (!TryReadMagicChunk(raw, blockPosition, out var compressedSize, out var decompressedSize, out var zlibPosition))
            {
                continue;
            }

            try
            {
                var chunk = DecompressZlibChunk(raw, zlibPosition, compressedSize, decompressedSize);
                output.Write(chunk, 0, chunk.Length);
            }
            catch
            {
            }
        }

        return output.ToArray();
    }

    private static bool TryReadMagicChunk(byte[] raw, int blockPosition, out int compressedSize, out int decompressedSize, out int zlibPosition)
    {
        compressedSize = 0;
        decompressedSize = 0;
        zlibPosition = -1;

        if (blockPosition + ChunkHeaderSize > raw.Length)
        {
            return false;
        }

        compressedSize = BitConverter.ToInt32(raw, blockPosition + 16);
        decompressedSize = BitConverter.ToInt32(raw, blockPosition + 24);
        if (!IsValidChunkSize(compressedSize) || !IsValidChunkSize(decompressedSize))
        {
            return false;
        }

        var dataStart = blockPosition + ChunkHeaderSize;
        zlibPosition = FindZlibHeader(raw, dataStart, Math.Min(dataStart + ChunkSearchWindow, raw.Length));
        if (zlibPosition < 0 || zlibPosition + compressedSize > raw.Length)
        {
            return false;
        }

        return true;
    }

    private static byte[] TryDecompressChunkedUstbinLegacy(byte[] raw, string name)
    {
        var firstZlibPos = FindZlibHeader(raw, 0);
        if (firstZlibPos < 16)
        {
            return [];
        }

        var chunkMetadata = new List<(int compressedSize, int decompressedSize)>();
        var metaPos = 16;
        while (metaPos + ChunkMetadataStride <= firstZlibPos)
        {
            var compressedSize = BitConverter.ToInt32(raw, metaPos);
            var decompressedSize = BitConverter.ToInt32(raw, metaPos + 8);

            if (IsValidChunkSize(compressedSize) && IsValidChunkSize(decompressedSize))
            {
                chunkMetadata.Add((compressedSize, decompressedSize));
            }
            else if (compressedSize == 0 && decompressedSize == 0)
            {
                break;
            }

            metaPos += ChunkMetadataStride;
        }

        if (chunkMetadata.Count == 0)
        {
            return [];
        }

        using var output = new MemoryStream();
        var currentDataPos = firstZlibPos;
        for (var i = 0; i < chunkMetadata.Count; i++)
        {
            var (compressedSize, expectedDecompressedSize) = chunkMetadata[i];
            if (currentDataPos + compressedSize > raw.Length)
            {
                break;
            }

            if (!IsZlibHeader(raw, currentDataPos))
            {
                var foundZlib = FindZlibHeader(raw, currentDataPos, Math.Min(raw.Length, currentDataPos + LegacySearchWindow));
                if (foundZlib < 0)
                {
                    break;
                }

                currentDataPos = foundZlib;
            }

            try
            {
                var chunk = DecompressZlibChunk(raw, currentDataPos, compressedSize, expectedDecompressedSize);
                output.Write(chunk, 0, chunk.Length);
                currentDataPos += compressedSize;
            }
            catch
            {
                break;
            }
        }

        return output.ToArray();
    }

    private static bool IsValidChunkSize(int size) => size > 0 && size < MaxUstbinChunkSize;

    private static int FindZlibHeader(byte[] raw, int start, int? end = null)
    {
        var max = Math.Min(end ?? raw.Length, raw.Length);
        for (var i = Math.Max(0, start); i < max - 1; i++)
        {
            if (IsZlibHeader(raw, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsZlibHeader(byte[] raw, int offset)
    {
        if (offset + 1 >= raw.Length)
        {
            return false;
        }

        var cmf = raw[offset];
        var flg = raw[offset + 1];
        return cmf == 0x78 && ((cmf << 8) | flg) % 31 == 0;
    }

    private static byte[] DecompressZlibChunk(byte[] raw, int zlibPos, int compressedSize, int? expectedDecompressedSize)
    {
        if (compressedSize <= 2)
        {
            return [];
        }

        using var compressedStream = new MemoryStream(raw, zlibPos + 2, compressedSize - 2, false);
        using var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var output = expectedDecompressedSize is > 0 ? new MemoryStream(expectedDecompressedSize.Value) : new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }
}
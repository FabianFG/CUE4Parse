using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Readers.FArchive;

namespace CUE4Parse.GameTypes.DFHO.Assets.Objects;

public class FDeltaStringTable
{
    public string TableNamespace;
    public Dictionary<string, string> KeysToEntries;

    public FDeltaStringTable(FArchive Ar)
    {
        using var resultStream = new MemoryStream();
        while (Ar.Position < Ar.Length)
        {
            var initialPos = Ar.Position;
            Ar.Read<FCompressedChunkInfo>();
            var summary = Ar.Read<FCompressedChunkInfo>();
            Ar.Position = initialPos;

            var uncompressedChunk = new byte[summary.UncompressedSize];
            Ar.SerializeCompressedNew(uncompressedChunk, uncompressedChunk.Length, "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);

            resultStream.Write(uncompressedChunk, 0, uncompressedChunk.Length);
        }

        var fullUncompressed = resultStream.ToArray();
        // UStringTable without KeysToMetaData
        var tableAr = new FByteArchive("FDeltaStringTable", fullUncompressed, Ar.Versions);

        tableAr.Position += 4; // Size
        TableNamespace = tableAr.ReadFString();
        KeysToEntries = tableAr.ReadMap(tableAr.ReadFString, tableAr.ReadFString);
    }
}

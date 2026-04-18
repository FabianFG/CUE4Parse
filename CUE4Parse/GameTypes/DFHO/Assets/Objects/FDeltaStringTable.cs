using System.Collections.Generic;
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
        var initialPos = Ar.Position;
        Ar.Read<FCompressedChunkInfo>();
        var summary = Ar.Read<FCompressedChunkInfo>();
        Ar.Position = initialPos;

        var uncompressed = new byte[summary.UncompressedSize];
        Ar.SerializeCompressedNew(uncompressed, uncompressed.Length, "Zlib", ECompressionFlags.COMPRESS_NoFlags, false, out _);

        // UStringTable without KeysToMetaData
        var tableAr = new FByteArchive("FDeltaStringTable", uncompressed, Ar.Versions);

        tableAr.Position += 4; // Size
        TableNamespace = tableAr.ReadFString();
        KeysToEntries = tableAr.ReadMap(tableAr.ReadFString, tableAr.ReadFString);
    }
}

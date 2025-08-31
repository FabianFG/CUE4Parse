using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.ABI.Encryption.Aes;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] ABIExtract(FArchive reader, FPakEntry pakEntry)
    {
        if (pakEntry.IsCompressed)
        {
            var uncompressed = new byte[(int) pakEntry.UncompressedSize];
            var uncompressedOff = 0;
            foreach (var block in pakEntry.CompressionBlocks)
            {
                var blockSize = (int) block.Size;
                var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
                var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += (int) pakEntry.CompressionBlockSize;
            }

            return pakEntry.Extension switch
            {
                "ini" => ABIDecryption.AbiDecryptIni(uncompressed),
                //"lua" => ABIDecryption.AbiDecryptLua(uncompressed),
                "uasset" or "umap" => ABIDecryption.AbiDecryptPackageSummary(uncompressed),
                _ => uncompressed
            };
        }

        var size = (int) pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize, size, reader, pakEntry.IsEncrypted);
        data = pakEntry.Extension switch
        {
            "ini" => ABIDecryption.AbiDecryptIni(data),
            //"lua" => ABIDecryption.AbiDecryptLua(data),
            "uasset" or "umap" => ABIDecryption.AbiDecryptPackageSummary(data),
            _ => data
        };
        return size != pakEntry.UncompressedSize ? data.SubByteArray((int) pakEntry.UncompressedSize) : data;
    }
}

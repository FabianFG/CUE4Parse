using CUE4Parse.Compression;
using CUE4Parse.GameTypes.Century.Encryption;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] CenturyExtract(FArchive reader, FPakEntry pakEntry)
    {
        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        var uncompressedOff = 0;
        foreach (var block in pakEntry.CompressionBlocks)
        {
            var srcSize = (int) block.Size;
            var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
            CenturyDecryptPWC.CenturyDecrypt(compressed);
            var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
            Decompress(compressed, 0, srcSize, uncompressed, uncompressedOff, uncompressedSize,  CompressionMethod.LZ4);
            uncompressedOff += (int) pakEntry.CompressionBlockSize;
        }

        return uncompressed;
    }
}

using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public static readonly byte[] DQXIDecodeKey = [0x21, 0x52, 0x05, 0x21, 0x41, 0x10, 0x35, 0x01];

    private void DQXIReadIndexLegacy(StringComparer pathComparer, FArchive index)
    {
        if (MountPoint == "Game/Content/") MountPoint = "JackGame/Content/";
        byte[] xorKey = [0xDE, 0x00, 0xAD, 0x00, 0xFA, 0x00, 0xDE, 0x00, 0xBE, 0x00, 0xEF, 0x00, 0xCA, 0x00, 0xFE, 0x00];
        var fileCount = index.Read<int>();
        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        for (var i = 0; i < fileCount; i++)
        {
            var length = -2 * index.Read<int>(); ;
            var encryptedBytes = index.ReadBytes(length);
            TensorUtils.Xor(encryptedBytes, xorKey);
            var path = string.Concat(MountPoint, Encoding.Unicode.GetString(encryptedBytes.AsSpan()[..^2]));
            if (path.StartsWith("Game")) path = "Jack" + path;
            var entry = new FPakEntry(this, path, index);
            if (entry is { IsDeleted: true, Size: 0 }) continue;
            if (entry.IsEncrypted) EncryptedFileCount++;
            files[path] = entry;
        }

        Files = files;
    }

    public byte[] DQXIExtract(FArchive reader, FPakEntry pakEntry)
    {
        if (pakEntry.IsCompressed)
        {
            var uncompressed = new byte[(int) pakEntry.UncompressedSize];
            var uncompressedOff = 0;
            foreach (var block in pakEntry.CompressionBlocks)
            {
                var srcSize = (int) block.Size;
                var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                TensorUtils.Xor(compressed, DQXIDecodeKey);
                var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                Decompress(compressed, 0, srcSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += (int) pakEntry.CompressionBlockSize;
            }

            return uncompressed;
        }

        var size = (int) pakEntry.UncompressedSize;
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize, size, reader, pakEntry.IsEncrypted);
        TensorUtils.Xor(data, (byte)0xFF);
        return size != pakEntry.UncompressedSize ? data.SubByteArray((int) pakEntry.UncompressedSize) : data;
    }
}

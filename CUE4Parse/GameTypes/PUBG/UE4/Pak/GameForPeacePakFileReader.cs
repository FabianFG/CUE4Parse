using System;
using System.Collections.Generic;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private static byte[] GameForPeaceIniDecrypt = [
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31,
        0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32,
        0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33,
        0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34,
        0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35,
        0x37, 0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
        0x38, 0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 
        0x39, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
    ];
    /// <summary>
    /// Function for extracting an entry from the pak in Game for Peace
    /// Ini files are encrypted with a simple xor cipher
    /// </summary>
    /// <param name="reader">The pak reader</param>
    /// <param name="pakEntry">The entry to be extracted</param>
    /// <returns>The merged and decompressed/decrypted entry data</returns>
    private byte[] GameForPeaceExtract(FArchive reader, FPakEntry pakEntry)
    {
        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        var uncompressedOff = 0;
        foreach (var block in pakEntry.CompressionBlocks)
        {
            var blockSize = (int) block.Size;
            var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
            // Read the compressed block
            var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
            // Calculate the uncompressed size,
            // its either just the compression block size,
            // or if it's the last block, it's the remaining data size
            var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
            Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
            uncompressedOff += (int) pakEntry.CompressionBlockSize;
        }
        if (pakEntry.Extension == "ini" && BitConverter.ToUInt64(uncompressed, 0) == 0x4b4457585d5d5b7d)
        {
            for (var i = 0; i < uncompressed.Length; i++)
            {
                uncompressed[i] ^= GameForPeaceIniDecrypt[i % GameForPeaceIniDecrypt.Length];
            }
        }
        return uncompressed;
    }

    private void GameForPeaceReadIndex(bool caseInsensitive, FByteArchive index)
    {
        var saved = index.Position;
        var pakentries = index.ReadArray(() => new FPakEntry(this, "", index, Game));
        var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) Ar.Read<long>()));
        var fileCount = pakentries.Length;
        var files = new Dictionary<string, GameFile>(pakentries.Length);
        var directoryIndexLength = (int) directoryIndex.Read<long>();
        index.Position = saved + 4;
        for (var i = 0; i < directoryIndexLength; i++)
        {
            var dir = directoryIndex.ReadFString();
            var dirDictLength = (int) directoryIndex.Read<long>();

            for (var j = 0; j < dirDictLength; j++)
            {
                var name = directoryIndex.ReadFString();
                string path;
                if (MountPoint.EndsWith('/') && dir.StartsWith('/'))
                    path = dir.Length == 1 ? string.Concat(MountPoint, name) : string.Concat(MountPoint, dir[1..], name);
                else
                    path = string.Concat(MountPoint, dir, name);

                var indexf = directoryIndex.Read<int>();

                pakentries[indexf].Path = path;
                files[caseInsensitive ? path.ToLowerInvariant() : path] = pakentries[indexf];
            }
        }
        Files = files;
    }
}

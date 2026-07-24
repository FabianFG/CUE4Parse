using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.SM4;
using CUE4Parse.GameTypes.Tencent.PUBGMobile.Lua;
using CUE4Parse.GameTypes.Tencent.PUBGMobile.UE4.Pak.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using ZstdSharp;
using UECompression = CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private Decompressor? _pubgMobileZstdDecompressor;

    private void PUBGMobileReadIndex(StringComparer pathComparer, FByteArchive index)
    {
        var entryCount = index.Read<int>();
        if (entryCount < 0)
            throw new ParserException(index, "Invalid PUBG Mobile pak entry count");

        var entries = new FPUBGMobilePakEntry[entryCount];
        for (var i = 0; i < entries.Length; i++)
        {
            // The entry uses the same field order as Game for Peace
            // but PUBG Mobile also stores per-entry encryption method and key id
            entries[i] = new FPUBGMobilePakEntry(this, index);
        }

        var directoryCount64 = index.Read<long>();
        if (directoryCount64 is < 0 or > int.MaxValue)
            throw new ParserException(index, "Invalid PUBG Mobile directory count");

        var files = new Dictionary<string, GameFile>(entryCount, pathComparer);
        for (var directoryIndex = 0; directoryIndex < (int) directoryCount64; directoryIndex++)
        {
            var directory = index.ReadFString();
            var fileCount64 = index.Read<long>();
            if (fileCount64 is < 0 or > int.MaxValue)
                throw new ParserException(index, "Invalid PUBG Mobile directory file count");

            for (var fileIndex = 0; fileIndex < (int) fileCount64; fileIndex++)
            {
                var name = index.ReadFString();
                var serializedEntryIndex = index.Read<int>();
                var entryIndex = -(long) serializedEntryIndex - 1;
                if (entryIndex < 0 || entryIndex >= entries.Length)
                    throw new ParserException(index, $"Invalid PUBG Mobile pak entry index {serializedEntryIndex}");

                string path;
                if (MountPoint.EndsWith('/') && directory.StartsWith('/'))
                    path = directory.Length == 1 ? string.Concat(MountPoint, name) : string.Concat(MountPoint, directory.AsSpan(1), name);
                else
                    path = string.Concat(MountPoint, directory, name);

                var entry = entries[entryIndex];
                entry.Path = path;
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
            }
        }

        Files = files;

        if (entries.Any(entry => entry.CustomData is 152))
            InitializePUBGMobileZstdDictionary();
    }

    private byte[] PUBGMobileExtract(FArchive Ar, FPakEntry entry, FByteBulkDataHeader? header)
    {
        if (entry is not FPUBGMobilePakEntry pakEntry)
            throw new ParserException("Invalid PUBG Mobile pak entry type");

        var offset = header?.OffsetInFile ?? 0;
        var requestedSize = checked((int) (header?.SizeOnDisk ?? pakEntry.UncompressedSize));
        if (offset < 0 || offset > pakEntry.UncompressedSize - requestedSize)
            throw new ParserException($"Invalid PUBG Mobile extraction range {offset}..{offset + requestedSize} for {pakEntry.UncompressedSize}-byte entry");
        if (requestedSize == 0)
            return [];

        var alignment = pakEntry.IsEncrypted ? Aes.ALIGN : 1;
        if (!pakEntry.IsCompressed)
        {
            var readOffset = offset & ~((long) alignment - 1);
            var dataOffset = checked((int) (offset - readOffset));
            var readSize = checked((dataOffset + requestedSize).Align(alignment));
            var data = Ar.ReadBytesAt(pakEntry.Offset + readOffset, readSize);
            if (pakEntry.IsEncrypted)
                data = PUBGMobileSM4.Decrypt(data, 0, data.Length, pakEntry.Path, pakEntry.EncryptionMethod, pakEntry.EncryptionKeyId);

            var output = dataOffset == 0 && requestedSize == data.Length
                ? data
                : data.AsSpan(dataOffset, requestedSize).ToArray();

            return pakEntry.Extension switch
            {
                "lua" => PUBGMobileLua.DecryptLuaBytecode(pakEntry.Name, output, Ar.Game),
                _ => output
            };
        }

        var compressionBlockSize = checked((int) pakEntry.CompressionBlockSize);
        if (compressionBlockSize <= 0)
            throw new ParserException("Invalid PUBG Mobile compression block size");

        var firstBlockIndex = checked((int) (offset / compressionBlockSize));
        var lastBlockIndex = checked((int) ((offset + requestedSize - 1) / compressionBlockSize));
        if ((uint) lastBlockIndex >= (uint) pakEntry.CompressionBlocks.Length)
            throw new ParserException($"Invalid PUBG Mobile compression block index {lastBlockIndex}");

        var firstBlockOffset = (long) firstBlockIndex * compressionBlockSize;
        var endBlockOffset = Math.Min(pakEntry.UncompressedSize, (long) (lastBlockIndex + 1) * compressionBlockSize);
        var uncompressed = new byte[checked((int) (endBlockOffset - firstBlockOffset))];

        for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
        {
            // Salt encrypted PUBG Mobile entries store compressed blocks in a shuffled order to make life harder
            var serializedBlockIndex = pakEntry.EncryptionMethod is EPUBGMobileEncryptionMethod.LiteSaltSM4 or
                    >= EPUBGMobileEncryptionMethod.SaltSM4Min and <= EPUBGMobileEncryptionMethod.SaltSM4Max
                ? PUBGUnshuffleBlockIndex(blockIndex, pakEntry.CompressionBlocks.Length)
                : blockIndex;

            var block = pakEntry.CompressionBlocks[serializedBlockIndex];
            var compressedSize = checked((int) block.Size);
            var readSize = compressedSize.Align(alignment);
            var compressed = Ar.ReadBytesAt(block.CompressedStart, readSize);
            if (pakEntry.IsEncrypted)
                compressed = PUBGMobileSM4.Decrypt(compressed, 0, compressed.Length, pakEntry.Path, pakEntry.EncryptionMethod, pakEntry.EncryptionKeyId);

            var globalBlockOffset = (long) blockIndex * compressionBlockSize;
            var destinationSize = checked((int) Math.Min(compressionBlockSize, pakEntry.UncompressedSize - globalBlockOffset));
            var destinationOffset = checked((int) (globalBlockOffset - firstBlockOffset));
            var destination = uncompressed.AsSpan(destinationOffset, destinationSize);

            if (pakEntry.CustomData is 152)
            {
                var zstd = _pubgMobileZstdDecompressor ?? throw new ParserException("PUBG Mobile ZSTD dictionary is not initialized");
                bool decompressed;
                int written;
                lock (zstd)
                    decompressed = zstd.TryUnwrap(compressed.AsSpan(0, compressedSize), destination, out written);

                if (!decompressed || written != destinationSize)
                    throw new ParserException($"Failed to decompress PUBG Mobile dictionary-ZSTD data (Expected: {destinationSize}, Result: {written})");
            }
            else
            {
                UECompression.Decompress(compressed.AsSpan(0, compressedSize), destination, pakEntry.CompressionMethod, Ar);
            }
        }

        var offsetInFirstBlock = checked((int) (offset - firstBlockOffset));

        var uncompressedOutput = offsetInFirstBlock == 0 && requestedSize == uncompressed.Length
            ? uncompressed
            : uncompressed.AsSpan(offsetInFirstBlock, requestedSize).ToArray();

        return pakEntry.Extension switch
        {
            "lua" => PUBGMobileLua.DecryptLuaBytecode(pakEntry.Name, uncompressedOutput, Ar.Game),
            _ => uncompressedOutput
        };
    }

    // sub_A1109D8
    private static int PUBGUnshuffleBlockIndex(int logicalBlockIndex, int blockCount)
    {
        Span<int> shuffled = blockCount <= 128 ? stackalloc int[blockCount] : new int[blockCount];
        var state = blockCount;
        for (var shuffledIndex = 0; shuffledIndex < blockCount; shuffledIndex++)
        {
            int candidate;
            do
            {
                var multiplied = unchecked(0x41C64E6D * state);
                state = unchecked(multiplied + 0x3039);
                var randomValue = state >= 0 ? state : unchecked(multiplied + 0x13038);
                candidate = (int) (((uint) (randomValue >> 16) % 0x7FFFu) % (uint) blockCount);
            } while (shuffled[..shuffledIndex].Contains(candidate));

            shuffled[shuffledIndex] = candidate;
        }

        for (var serializedBlockIndex = 0; serializedBlockIndex < blockCount; serializedBlockIndex++)
        {
            if (shuffled[serializedBlockIndex] == logicalBlockIndex)
                return serializedBlockIndex;
        }

        throw new InvalidOperationException("Failed to reconstruct PUBG Mobile compression block order");
    }

    // ZSTD dictionary located in mini_obbzsdic_obb.pak
    private void InitializePUBGMobileZstdDictionary()
    {
        var dictionaryEntry = Files.Values
            .OfType<FPUBGMobilePakEntry>()
            .FirstOrDefault(entry => entry.Path.EndsWith("Content/Config/zstddic/mini_obbzsdic_obb", StringComparison.OrdinalIgnoreCase));

        if (dictionaryEntry == null || dictionaryEntry.UncompressedSize <= 16 || dictionaryEntry.UncompressedSize > int.MaxValue)
            throw new ParserException("PUBG Mobile ZSTD dictionary entry was not found");

        var wrappedDictionary = Ar.ReadBytesAt(dictionaryEntry.Offset, (int) dictionaryEntry.UncompressedSize);
        var dictionarySize = BitConverter.ToInt32(wrappedDictionary, 0);
        if (dictionarySize <= 0 || dictionarySize > wrappedDictionary.Length - 16 || BitConverter.ToInt32(wrappedDictionary, 12) != dictionarySize)
            throw new ParserException("Invalid PUBG Mobile ZSTD dictionary");

        _pubgMobileZstdDecompressor = new Decompressor();
        _pubgMobileZstdDecompressor.LoadDictionary(wrappedDictionary.AsSpan(16, dictionarySize));
    }
}

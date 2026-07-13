using CommunityToolkit.HighPerformance.Buffers;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using GenericReader;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private void ValorantSourceReadIndexUpdated(StringComparer pathComparer)
    {
        Ar.Position = Info.IndexOffset;
        using var primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecryptIndex((int) Info.IndexSize), Versions);

        var fileCount = primaryIndex.Read<int>();
        if (fileCount < 0)
            throw new ParserException(primaryIndex, "Invalid Valorant Source file count");

        primaryIndex.Position = 0x3C;
        var directoryIndexSize = primaryIndex.Read<long>();
        primaryIndex.Position = 0x4D;
        var directoryIndexOffset = primaryIndex.Read<long>();

        primaryIndex.Position = 0x5C;
        var mountPoint = primaryIndex.ReadFString();
        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;

        primaryIndex.Position += 4;

        var encodedPakEntriesSize = primaryIndex.Read<int>();
        if (encodedPakEntriesSize < 0 || encodedPakEntriesSize > primaryIndex.Length - primaryIndex.Position)
            throw new ParserException(primaryIndex, "Invalid Valorant Source encoded entry data size");

        using var encodedPakEntries = new GenericBufferReader(primaryIndex.ReadBytes(encodedPakEntriesSize));

        if (directoryIndexOffset < 0 || directoryIndexSize < 0 || directoryIndexOffset + directoryIndexSize > Ar.Length)
            throw new ParserException(primaryIndex, "Invalid Valorant Source directory index range");

        Ar.Position = directoryIndexOffset;
        using var directoryIndex = new GenericBufferReader(ReadAndDecryptIndex((int) directoryIndexSize));

        EncryptedFileCount = 0;
        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        const int poolLength = 256;
        var mountPointSpan = MountPoint.AsSpan();
        using var charsPool = SpanOwner<char>.Allocate(poolLength * 2);
        var dirPoolSpan = charsPool.Span[..poolLength];
        var fileNamePoolSpan = charsPool.Span[poolLength..];
        var directoryCount = directoryIndex.Read<int>();

        for (var directory = 0; directory < directoryCount; directory++)
        {
            var dirSpan = dirPoolSpan;
            var dir = directoryIndex.ReadFStringMemory();
            var dirLength = dir.GetEncoding().GetChars(dir.GetSpan(), dirSpan);
            var trimDir = !mountPointSpan.IsEmpty && dirLength > 0 && dirSpan[0] == '/' && mountPointSpan[^1] == '/';
            dirSpan = dirSpan[(trimDir ? 1 : 0)..dirLength];

            var entryCount = directoryIndex.Read<int>();
            for (var fileIndex = 0; fileIndex < entryCount; fileIndex++)
            {
                var fileNameSpan = fileNamePoolSpan;
                var fileName = directoryIndex.ReadFStringMemory();
                var fileNameLength = fileName.GetEncoding().GetChars(fileName.GetSpan(), fileNameSpan);
                var path = string.Concat(mountPointSpan, dirSpan, fileNameSpan[..fileNameLength]);
                var encodedOffset = directoryIndex.Read<int>();
                if (encodedOffset == int.MinValue)
                    continue;
                if (encodedOffset < 0)
                    throw new ParserException("Valorant Source index contains an unsupported non-encoded entry");

                var entry = new FPakEntry(this, path, encodedPakEntries, encodedOffset);
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
            }
        }

        Files = files;
    }
}

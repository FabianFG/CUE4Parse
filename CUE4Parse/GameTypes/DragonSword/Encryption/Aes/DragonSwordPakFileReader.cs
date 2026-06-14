using System.Text;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using GenericReader;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    private void DragonSwordReadIndexUpdated(StringComparer pathComparer)
    {
        string ReadString(FArchive Ar)
        {
            var len = Ar.Read<int>();
            if (len == 0) return "";

            if (len > 0)
            {
                Span<byte> span = stackalloc byte[len];
                Ar.ReadExactly(span);
                var xorKey = span[^1];
                for (var i = 0; i < len; i++)
                    span[i] ^= xorKey;
                return Encoding.UTF8.GetString(span[..^1]);
            }
            
            {
                len = -len;
                Span<char> span = stackalloc char[len];
                Ar.ReadExactly(span.Cast<char, byte>());
                var xorKey = span[^1];
                for (var i = 0; i < len; i++)
                    span[i] ^= xorKey;
                return Encoding.Unicode.GetString(span[..^1].Cast<char, byte>());
            }
        }

        // Prepare primary index and decrypt if necessary
        Ar.Position = Info.IndexOffset;
        this.bDecrypted = true;
        var indexdata = ReadAndDecryptIndex((int) Info.IndexSize);
        using FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", indexdata);

        
        EncryptedFileCount = 0;

        string mountPoint;
        try
        {
            mountPoint = ReadString(primaryIndex);
        }
        catch (Exception e)
        {
            throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
        }

        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;
        var fileCount = primaryIndex.Read<int>();
        primaryIndex.Position += 8; // PathHashSeed

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No path hash index");

        primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No directory index");

        var directoryIndexOffset = primaryIndex.Read<long>();
        var directoryIndexSize = primaryIndex.Read<long>();
        primaryIndex.Position += 20; // Directory Index hash
        var encodedPakEntriesSize = primaryIndex.Read<int>();
        var encodedPakEntriesData = primaryIndex.ReadBytes(encodedPakEntriesSize);
        if (encodedPakEntriesSize > 0)
        {
            var xorByte = encodedPakEntriesData[5];
            for (var i = 0; i < encodedPakEntriesSize; i++)
                encodedPakEntriesData[i] ^= xorByte;
        }
        using var encodedPakEntries = new GenericBufferReader(encodedPakEntriesData);

        var FilesNum = primaryIndex.Read<int>();
        if (FilesNum < 0)
            throw new ParserException("Corrupt pak PrimaryIndex detected");

        var NonEncodedEntries = primaryIndex.ReadArray(FilesNum, () => new FPakEntry(this, "", primaryIndex));

        // Read FDirectoryIndex
        Ar.Position = directoryIndexOffset;
        var data = ReadAndDecryptIndex((int) directoryIndexSize);           

        using var directoryIndex = new FByteArchive($"{Name} - directory index", data);

        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);

        const int poolLength = 256;
        var mountPointSpan = MountPoint.AsSpan();
        using var charsPool = SpanOwner<char>.Allocate(poolLength * 2);
        var charsSpan = charsPool.Span;
        var dirPoolSpan = charsSpan[..poolLength];
        var fileNamePoolSpan = charsSpan[poolLength..];
        var directoryIndexLength = directoryIndex.Read<int>();
        for (var dirIndex = 0; dirIndex < directoryIndexLength; dirIndex++)
        {
            var dirSpan = dirPoolSpan;
            var dir = ReadString(directoryIndex);
            dir.CopyTo(dirSpan);
            var dirLength = dir.Length;
            var trimDir = !mountPointSpan.IsEmpty && dirSpan[0] == '/' && mountPointSpan[^1] == '/';
            dirSpan = dirSpan[(trimDir ? 1 : 0)..dirLength];

            var fileEntries = directoryIndex.Read<int>();
            for (var fileIndex = 0; fileIndex < fileEntries; fileIndex++)
            {
                var fileNameSpan = fileNamePoolSpan;
                var fileName = ReadString(directoryIndex);
                fileName.CopyTo(fileNameSpan);
                var fileNameLength = fileName.Length;
                
                fileNameSpan = fileNameSpan[..fileNameLength];
                var path = string.Concat(mountPointSpan, dirSpan, fileNameSpan);

                var offset = directoryIndex.Read<int>();
                if (offset == int.MinValue)
                    continue;

                FPakEntry entry;
                if (offset >= 0)
                {
                    entry = new FPakEntry(this, path, encodedPakEntries, offset);
                }
                else
                {
                    var index = -offset - 1;
                    if (index < 0 || index >= NonEncodedEntries.Length)
                    {
                        Log.Warning("Invalid nonencoded pak entry with index {Index}, path {Path}", index, path);
                        continue;
                    }

                    entry = NonEncodedEntries[index];
                    entry.Path = path;
                }
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                files[path] = entry;
            }
        }

        Files = files;
    }
}

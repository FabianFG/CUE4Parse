using System.Buffers;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.MarchOfGiants.Encryption;
using CUE4Parse.GameTypes.MarchOfGiants.FileProvider.Objects;
using CUE4Parse.GameTypes.MarchOfGiants.Vfs.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.GameTypes.MarchOfGiants.Vfs;

public sealed class MogFileReader : AbstractAesVfsReader
{
    private const int HeaderSize = 0x70;
    private static ReadOnlySpan<byte> Magic => "M0Giants"u8;
    private readonly FArchive _archive;
    private readonly byte[] _header;
    private readonly int _cacheSizeLimit = 128 * 1024 * 1024;
    private readonly Dictionary<FMogBlock, LinkedListNode<FMogCachedBlock>> _blockCache = [];
    private readonly LinkedList<FMogCachedBlock> _cacheOrder = new();
    private long _cachedBytes;

    public override long Length { get; set; }
    public override string MountPoint { get; protected set; } = "";
    public override bool HasDirectoryIndex => true;
    public override FGuid EncryptionKeyGuid => default;
    public override bool IsEncrypted => false;

    public MogFileReader(FArchive Ar) : base(System.IO.Path.ChangeExtension(Ar.Name, ".pak"), Ar.Versions)
    {
        _archive = Ar;
        Length = Ar.Length;
        CompressionMethods = [CompressionMethod.None, CompressionMethod.Zstd];

        var encrypted = ReadBytesAt(0, HeaderSize);

        // Basically it's guessing key from key pool
        var probe = new byte[Aes.ALIGN];
        for (var index = 0; index < MogEncryption.KeyCount; index++)
        {
            var key = MogEncryption.GetHeaderKey(index);
            encrypted.AsSpan(0, Aes.ALIGN).CopyTo(probe);
            probe.DecryptInPlace(key);
            if (!probe.AsSpan(0, Magic.Length).SequenceEqual(Magic))
                continue;

            encrypted.DecryptInPlace(key);
            _header = encrypted;
            return;
        }

        throw new ParserException(Ar, "No valid MOG header key found");
    }

    public override void Mount(StringComparer pathComparer)
    {
        ClearCache();
        var blocks = ReadBlockIndex();
        using var toc = new FByteArchive(Name, DecodeBlock(blocks.Values.First()), Versions);
        var fileCount = toc.Read<int>();
        if (fileCount < 0)
            throw new ParserException(toc, "Invalid MOG file count");

        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        for (var i = 0; i < fileCount; i++)
        {
            var entry = new FMogEntry(this, toc, blocks);
            files[entry.Path] = entry;
        }

        Files = files;
        EncryptedFileCount = files.Count;
        bDecrypted = true;
    }

    private Dictionary<FGuid, FMogBlock> ReadBlockIndex()
    {
        using var header = new FByteArchive(Name, _header, Versions) { Position = 0x40 };
        var indexBlock = new FMogBlock(header, HeaderSize);
        using var index = new FByteArchive(Name, DecodeBlock(indexBlock), Versions);
        var blockCount = index.Read<int>();
        if (blockCount <= 0 || blockCount > (index.Length - index.Position) / 40)
            throw new ParserException(index, "Invalid MOG block count");

        var dataOffset = (long) HeaderSize + indexBlock.EncryptedSize;
        var blocks = new Dictionary<FGuid, FMogBlock>(blockCount);
        for (var i = 0; i < blockCount; i++)
        {
            var block = new FMogBlock(index, dataOffset + index.Read<long>());
            blocks.TryAdd(block.Hash, block);
        }

        return blocks;
    }

    private byte[] DecodeBlock(FMogBlock block)
    {
        var encryptedSize = block.EncryptedSize;
        var data = block.IsCompressed ? ArrayPool<byte>.Shared.Rent(encryptedSize) : new byte[encryptedSize];
        try
        {
            ReadExactlyAt(block.Offset, data, 0, encryptedSize);
            data.DecryptInPlace(0, encryptedSize, block.Key);

            if (block.IsCompressed)
                return Decompress(data, 0, block.CompressedSize, block.Size, CompressionMethod.Zstd, _archive);
            if (data.Length != block.Size)
                Array.Resize(ref data, block.Size);

            return data;
        }
        finally
        {
            if (block.IsCompressed)
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }
    }

    internal byte[] GetBlock(FMogBlock block)
    {
        if (block.Size > _cacheSizeLimit)
            return DecodeBlock(block);

        LinkedListNode<FMogCachedBlock> node;
        lock (_blockCache)
        {
            if (_blockCache.TryGetValue(block, out node!))
            {
                if (node != _cacheOrder.First)
                {
                    _cacheOrder.Remove(node);
                    _cacheOrder.AddFirst(node);
                }
            }
            else
            {
                while (_cachedBytes + block.Size > _cacheSizeLimit && _cacheOrder.Last is { } oldest)
                {
                    _blockCache.Remove(oldest.Value.Block);
                    _cachedBytes -= oldest.Value.Block.Size;
                    _cacheOrder.RemoveLast();
                }
                node = new LinkedListNode<FMogCachedBlock>(new(block, new Lazy<byte[]>(() => DecodeBlock(block))));
                _blockCache.Add(block, node);
                _cachedBytes += block.Size;
                _cacheOrder.AddFirst(node);
            }
        }

        // Share cache misses without holding the cache lock (it's slow otherwise during concurrent access)
        return node.Value.Data.Value;
    }

    internal void ReadRawRange(FMogBlock block, int offset, byte[] result, int destination, int count)
    {
        if (offset % Aes.ALIGN == 0 && count % Aes.ALIGN == 0)
        {
            ReadExactlyAt(block.Offset + offset, result, destination, count);
            result.DecryptInPlace(destination, count, block.Key);
            return;
        }

        var alignedOffset = offset - offset % Aes.ALIGN;
        var encryptedSize = (int) ((long) offset - alignedOffset + count).Align(Aes.ALIGN);
        var buffer = ArrayPool<byte>.Shared.Rent(encryptedSize);
        try
        {
            ReadExactlyAt(block.Offset + alignedOffset, buffer, 0, encryptedSize);
            buffer.DecryptInPlace(0, encryptedSize, block.Key);
            Buffer.BlockCopy(buffer, offset - alignedOffset, result, destination, count);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private byte[] ReadBytesAt(long offset, int count)
    {
        var result = new byte[count];
        ReadExactlyAt(offset, result, 0, count);
        return result;
    }

    private void ReadExactlyAt(long offset, byte[] buffer, int destination, int count)
    {
        var read = 0;
        while (read < count)
        {
            var size = _archive.ReadAt(offset + read, buffer, destination + read, count - read);
            if (size <= 0)
                throw new EndOfStreamException("Truncated MOG block");

            read += size;
        }
    }

    public override byte[] Extract(VfsEntry entry, FByteBulkDataHeader? header = null)
    {
        if (entry is not FMogEntry mogEntry || entry.Vfs != this)
            throw new ArgumentException("Entry does not belong to this MOG archive", nameof(entry));

        return mogEntry.Read(header);
    }

    private void ClearCache()
    {
        lock (_blockCache)
        {
            _blockCache.Clear();
            _cacheOrder.Clear();
            _cachedBytes = 0;
        }
    }

    public override byte[] MountPointCheckBytes() => [];
    protected override byte[] ReadAndDecrypt(int length) => throw new NotSupportedException("MOG decryption requires a block descriptor");

    public override void Dispose()
    {
        ClearCache();
        _archive.Dispose();
    }
}

using System;
using System.Buffers;
using System.IO;

using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.UE4.Readers;

/// <summary>
/// A stream that reads compressed archive entries on-demand, block by block.
/// Only keeps one decompressed block in memory at a time.
/// </summary>
public sealed class StreamingAssetReader : Stream
{
    private readonly IBlockProvider _blockProvider;
    private readonly FAesKey? _aesKey;
    private readonly IAesVfsReader.CustomEncryptionDelegate? _customDecryption;
    private readonly IAesVfsReader? _vfsReader;
    private readonly long _offsetInFirstBlock;

    // Current block cache
    private int _currentBlockIndex = -1;
    private byte[]? _currentBlockData;
    private int _currentBlockDataSize;
    private long _currentBlockStart;
    private long _currentBlockEnd;
    private long _position;
    private bool _disposed;

    /// <summary>
    /// Creates a streaming reader for a Pak entry.
    /// </summary>
    /// <param name="blockProvider">Block provider for the entry.</param>
    /// <param name="aesKey">Optional AES key for decryption.</param>
    public StreamingAssetReader(IBlockProvider blockProvider, FAesKey? aesKey)
        : this(blockProvider, aesKey, null, null, 0)
    {
    }

    /// <summary>
    /// Creates a streaming reader for an IoStore entry.
    /// </summary>
    /// <param name="blockProvider">Block provider for the entry.</param>
    /// <param name="aesKey">Optional AES key for decryption.</param>
    /// <param name="offsetInFirstBlock">Offset within the first block where entry data starts.</param>
    public StreamingAssetReader(IBlockProvider blockProvider, FAesKey? aesKey, long offsetInFirstBlock)
        : this(blockProvider, aesKey, null, null, offsetInFirstBlock)
    {
    }

    /// <summary>
    /// Creates a streaming reader with custom decryption support.
    /// </summary>
    /// <param name="blockProvider">Block provider for the entry.</param>
    /// <param name="aesKey">Optional AES key for decryption.</param>
    /// <param name="customDecryption">Optional custom decryption delegate.</param>
    /// <param name="vfsReader">VFS reader for custom decryption context.</param>
    /// <param name="offsetInFirstBlock">Offset within the first block where entry data starts (IoStore only).</param>
    public StreamingAssetReader(
        IBlockProvider blockProvider,
        FAesKey? aesKey,
        IAesVfsReader.CustomEncryptionDelegate? customDecryption,
        IAesVfsReader? vfsReader,
        long offsetInFirstBlock = 0)
    {
        _blockProvider = blockProvider ?? throw new ArgumentNullException(nameof(blockProvider));
        _aesKey = aesKey;
        _customDecryption = customDecryption;
        _vfsReader = vfsReader;
        _offsetInFirstBlock = offsetInFirstBlock;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _blockProvider.UncompressedSize;
    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be negative.");
            _position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StreamingAssetReader));
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (buffer.Length - offset < count)
            throw new ArgumentException("Invalid offset and count for buffer length.");

        var totalRead = 0;
        while (count > 0 && _position < Length)
        {
            // Check if position is within cached block
            if (_position < _currentBlockStart || _position >= _currentBlockEnd)
            {
                var blockIndex = _blockProvider.FindBlockForOffset(_position);
                if (blockIndex < 0)
                    break;
                LoadBlock(blockIndex);
            }

            // Calculate how much we can read from current block
            var blockOffset = (int)(_position - _currentBlockStart);
            var available = (int)(_currentBlockEnd - _position);
            // Clamp to remaining entry length to prevent reading past entry end
            var toRead = (int)Math.Min(Math.Min(count, available), Length - _position);

            Buffer.BlockCopy(_currentBlockData!, blockOffset, buffer, offset, toRead);
            _position += toRead;
            offset += toRead;
            count -= toRead;
            totalRead += toRead;
        }

        return totalRead;
    }

    private void LoadBlock(int blockIndex)
    {
        ReturnCurrentBlock();

        var block = _blockProvider.GetBlock(blockIndex);
        var readSize = _blockProvider.GetBlockReadSize(blockIndex);

        // Rent buffer for raw (compressed/encrypted) data
        var rawBuffer = ArrayPool<byte>.Shared.Rent(readSize);
        try
        {
            // Read raw block data
            _blockProvider.ReadBlockRaw(blockIndex, rawBuffer, 0);

            // Decrypt if needed
            byte[] decrypted;
            if (_blockProvider.IsEncrypted)
            {
                decrypted = DecryptBlock(rawBuffer, readSize);
            }
            else
            {
                decrypted = rawBuffer;
            }

            // Decompress if needed - use per-block method for IoStore support
            var compressionMethod = _blockProvider.GetBlockCompressionMethod(blockIndex);
            if (compressionMethod != CompressionMethod.None)
            {
                _currentBlockData = ArrayPool<byte>.Shared.Rent(block.UncompressedSize);
                Compression.Compression.Decompress(
                    decrypted, 0, block.CompressedSize,
                    _currentBlockData, 0, block.UncompressedSize,
                    compressionMethod);
                _currentBlockDataSize = block.UncompressedSize;

                // If we decrypted to a new buffer (not rawBuffer), return it
                if (decrypted != rawBuffer)
                {
                    // Decrypted buffer is a new allocation from AES, not pooled
                }
            }
            else
            {
                // Uncompressed: use raw data directly
                if (decrypted != rawBuffer)
                {
                    // Decrypted buffer is a new allocation
                    _currentBlockData = ArrayPool<byte>.Shared.Rent(block.UncompressedSize);
                    Buffer.BlockCopy(decrypted, 0, _currentBlockData, 0, block.UncompressedSize);
                    _currentBlockDataSize = block.UncompressedSize;
                }
                else
                {
                    // Keep the raw buffer as current block data
                    _currentBlockData = rawBuffer;
                    _currentBlockDataSize = block.UncompressedSize;
                    rawBuffer = null!; // Prevent return in finally
                }
            }
        }
        finally
        {
            if (rawBuffer != null)
                ArrayPool<byte>.Shared.Return(rawBuffer);
        }

        _currentBlockIndex = blockIndex;

        // Calculate logical offsets for this block's data within the entry
        if (blockIndex == 0)
        {
            // First block: account for offset within block (IoStore)
            _currentBlockStart = 0;
            _currentBlockEnd = block.UncompressedSize - _offsetInFirstBlock;
        }
        else
        {
            // Subsequent blocks
            _currentBlockStart = block.UncompressedOffset;
            _currentBlockEnd = block.UncompressedOffset + block.UncompressedSize;
        }

        // Adjust the cached data to skip offset in first block
        if (blockIndex == 0 && _offsetInFirstBlock > 0)
        {
            // For IoStore: shift data to account for entry starting mid-block
            var shiftedSize = (int)(block.UncompressedSize - _offsetInFirstBlock);
            var shiftedBuffer = ArrayPool<byte>.Shared.Rent(shiftedSize);
            Buffer.BlockCopy(_currentBlockData!, (int)_offsetInFirstBlock, shiftedBuffer, 0, shiftedSize);

            var oldBuffer = _currentBlockData;
            _currentBlockData = shiftedBuffer;
            _currentBlockDataSize = shiftedSize;

            if (oldBuffer != null)
                ArrayPool<byte>.Shared.Return(oldBuffer);
        }
    }

    private byte[] DecryptBlock(byte[] data, int size)
    {
        if (_customDecryption != null && _vfsReader != null)
        {
            return _customDecryption(data, 0, size, false, _vfsReader);
        }

        if (_aesKey != null)
        {
            return Aes.Decrypt(data, 0, size, _aesKey);
        }

        return data;
    }

    private void ReturnCurrentBlock()
    {
        if (_currentBlockData != null)
        {
            ArrayPool<byte>.Shared.Return(_currentBlockData);
            _currentBlockData = null;
            _currentBlockDataSize = 0;
        }
        _currentBlockIndex = -1;
        _currentBlockStart = 0;
        _currentBlockEnd = 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentException("Invalid seek origin.", nameof(origin))
        };

        if (newPosition < 0)
            throw new IOException("Seek position cannot be negative.");

        _position = newPosition;
        return _position;
    }

    public override void Flush()
    {
        // No-op for read-only stream
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("StreamingAssetReader is read-only.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("StreamingAssetReader is read-only.");
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ReturnCurrentBlock();
                _blockProvider.Dispose();
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}

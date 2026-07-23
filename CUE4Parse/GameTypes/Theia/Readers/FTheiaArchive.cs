using CUE4Parse.GameTypes.Theia.Encryption;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Microsoft.Win32.SafeHandles;

namespace CUE4Parse.GameTypes.Theia.Readers;

public sealed class FTheiaArchive : FArchive
{
    private readonly SafeFileHandle _fileHandle;
    private readonly TheiaDecryptor _decryptor;

    public FTheiaArchive(string filePath, TheiaDecryptor decryptor, VersionContainer? versions) : base(versions)
    {
        Name = filePath;
        _fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.Asynchronous | FileOptions.RandomAccess);
        Length = RandomAccess.GetLength(_fileHandle);
        _decryptor = decryptor;
    }

    public FTheiaArchive(string filePath, VersionContainer? versions) : base(versions)
    {
        Name = filePath;
        _fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.Asynchronous | FileOptions.RandomAccess);
        Length = RandomAccess.GetLength(_fileHandle);
        var meta = File.ReadAllBytes(filePath + ".meta");
        _decryptor = new TheiaDecryptor(meta, Length);
    }

    public override string Name { get; }
    public override bool CanSeek => true;
    public override long Length { get; }
    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = ReadAt(Position, buffer);
        Position += bytesRead;
        return bytesRead;
    }

    public override int ReadAt(long position, byte[] buffer, int offset, int count) =>
        ReadAt(position, buffer.AsSpan(offset, count));

    public override int ReadAt(long position, Span<byte> buffer)
    {
        if (position > Length - buffer.Length)
            throw new EndOfStreamException($"Cannot read {position} bytes at 0x{position:X} from {Name} ({Length} bytes)");

        var bytesRead = RandomAccess.Read(_fileHandle, buffer, position);
        _decryptor.DecryptRangeInPlace(buffer[..bytesRead], position);
        return bytesRead;
    }

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) =>
        ReadAtAsync(position, buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken = default)
    {
        if (position > Length - memory.Length)
            throw new EndOfStreamException($"Cannot read {position} bytes at 0x{position:X} from {Name} ({Length} bytes)");

        var bytesRead = await RandomAccess.ReadAsync(_fileHandle, memory, position, cancellationToken).ConfigureAwait(false);
        _decryptor.DecryptRangeInPlace(memory.Span[..bytesRead], position);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (Position < 0)
            throw new IOException("Attempted to seek before the start of the archive");

        return Position;
    }

    public override object Clone() => new FTheiaArchive(Name, _decryptor, Versions) { Position = Position };

    public override void Close() => _fileHandle.Close();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_fileHandle.IsClosed)
            _fileHandle.Dispose();
    }
}

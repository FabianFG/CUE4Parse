using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Versions;

using Microsoft.Win32.SafeHandles;

using OffiUtils;

namespace CUE4Parse.UE4.Readers;

public class FStreamArchive : FArchive
{
    private readonly Stream _baseStream;

    public FStreamArchive(string name, Stream baseStream, VersionContainer? versions = null) : base(versions)
    {
        _baseStream = baseStream;
        Name = name;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin)
        => _baseStream.Seek(offset, origin);

    public override bool CanSeek => _baseStream.CanSeek;
    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override string Name { get; }

    public override object Clone()
    {
        return _baseStream switch
        {
            ICloneable cloneable => new FStreamArchive(Name, (Stream) cloneable.Clone(), Versions) {Position = Position},
            FileStream fileStream => new FStreamArchive(Name, File.Open(fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Versions) {Position = Position},
            _ => new FStreamArchive(Name, _baseStream, Versions) {Position = Position}
        };
    }

    public override void Close()
    {
        _baseStream.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
    }
}

public class FRandomAccessStreamArchive : FArchive
{
    private readonly RandomAccessStream _baseStream;

    public FRandomAccessStreamArchive(string name, RandomAccessStream baseStream, VersionContainer? versions = null) : base(versions)
    {
        _baseStream = baseStream;
        Name = name;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    public override int ReadAt(long position, byte[] buffer, int offset, int count)
        => _baseStream.ReadAt(position, buffer, offset, count);

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _baseStream.ReadAtAsync(position, buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken = default)
        => _baseStream.ReadAtAsync(position, memory, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin)
        => _baseStream.Seek(offset, origin);

    public override bool CanSeek => _baseStream.CanSeek;
    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override string Name { get; }

    public override object Clone() => new FRandomAccessStreamArchive(Name, _baseStream, Versions) {Position = Position};

    public override void Close()
    {
        _baseStream.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
    }
}

public class FRandomAccessFileStreamArchive : FArchive
{
    private readonly SafeFileHandle _fileHandle;

    public FRandomAccessFileStreamArchive(string filePath, VersionContainer? versions = null)
        : this(filePath, File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.Asynchronous), versions) { }

    public FRandomAccessFileStreamArchive(FileInfo fileInfo, VersionContainer? versions = null)
        : this(fileInfo.FullName, File.OpenHandle(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.Asynchronous), versions) { }

    public FRandomAccessFileStreamArchive(string filePath, SafeFileHandle fileHandle, VersionContainer? versions = null) : base(versions)
    {
        Name = filePath;
        _fileHandle = fileHandle;
        Length = RandomAccess.GetLength(fileHandle);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = RandomAccess.Read(_fileHandle, buffer, Position);
        Position += bytesRead;
        return bytesRead;
    }

    public override int ReadAt(long position, byte[] buffer, int offset, int count)
    {
        return ReadAt(position, buffer.AsSpan(offset, count));
    }

    public override int ReadAt(long position, Span<byte> buffer)
    {
        return RandomAccess.Read(_fileHandle, buffer, position);
    }

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        var memory = buffer.AsMemory(offset, count);
        return ReadAtAsync(position, memory, cancellationToken).AsTask();
    }

    public override ValueTask<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken = default)
    {
        return RandomAccess.ReadAsync(_fileHandle, memory, position, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException()
        };
        return Position;
    }

    public override bool CanSeek => true;

    public override long Length { get; }
    public override long Position { get; set; }

    public override string Name { get; }

    public override object Clone() => new FRandomAccessFileStreamArchive(Name, Versions) {Position = Position};

    public override void Close()
    {
        _fileHandle.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_fileHandle.IsClosed)
        {
            _fileHandle.Dispose();
        }
    }
}

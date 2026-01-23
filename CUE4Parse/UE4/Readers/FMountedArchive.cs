using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers;

/// <summary>
/// Memory-mapped file archive for efficient random access to large files.
/// Uses OS-level memory mapping for optimal I/O performance and reduced memory pressure.
/// Thread-safe for concurrent reads when cloned.
/// </summary>
public class FMountedArchive : FArchive
{
    private readonly MemoryMappedFile _mappedFile;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly bool _ownsFile;
    private long _position;

    public override string Name { get; }
    public override long Length { get; }
    public override bool CanSeek => true;

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

    /// <summary>
    /// Creates a memory-mapped archive from a file path.
    /// </summary>
    /// <param name="filePath">Path to the file to map.</param>
    /// <param name="versions">Version container for parsing.</param>
    public FMountedArchive(string filePath, VersionContainer? versions = null) : base(versions)
    {
        Name = filePath;
        var fileInfo = new FileInfo(filePath);
        Length = fileInfo.Length;

        _mappedFile = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            mapName: null,
            capacity: 0,
            MemoryMappedFileAccess.Read);

        _accessor = _mappedFile.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);
        _ownsFile = true;
    }

    /// <summary>
    /// Creates a memory-mapped archive from an existing FileInfo.
    /// </summary>
    public FMountedArchive(FileInfo file, VersionContainer? versions = null)
        : this(file.FullName, versions) { }

    /// <summary>
    /// Internal constructor for cloning - shares the memory-mapped file.
    /// </summary>
    private FMountedArchive(FMountedArchive source) : base(source.Versions)
    {
        Name = source.Name;
        Length = source.Length;
        _mappedFile = source._mappedFile;
        _accessor = source._accessor;
        _ownsFile = false;
        _position = source._position;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesToRead = (int)Math.Min(buffer.Length, Length - _position);
        if (bytesToRead <= 0)
            return 0;

        ReadInternal(_position, buffer[..bytesToRead]);
        _position += bytesToRead;
        return bytesToRead;
    }

    public override int ReadAt(long position, byte[] buffer, int offset, int count)
    {
        return ReadAt(position, buffer.AsSpan(offset, count));
    }

    public override int ReadAt(long position, Span<byte> buffer)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be negative.");

        var bytesToRead = (int)Math.Min(buffer.Length, Length - position);
        if (bytesToRead <= 0)
            return 0;

        ReadInternal(position, buffer[..bytesToRead]);
        return bytesToRead;
    }

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        // Memory-mapped reads are inherently fast, execute synchronously
        return Task.FromResult(ReadAt(position, buffer, offset, count));
    }

    public override ValueTask<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken = default)
    {
        // Memory-mapped reads are inherently fast, execute synchronously
        return new ValueTask<int>(ReadAt(position, memory.Span));
    }

    /// <summary>
    /// Thread-safe read using pointer access to the memory-mapped view.
    /// </summary>
    private unsafe void ReadInternal(long position, Span<byte> buffer)
    {
        byte* ptr = null;
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            new ReadOnlySpan<byte>(ptr + position, buffer.Length).CopyTo(buffer);
        }
        finally
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (newPosition < 0)
            throw new IOException("Seek position cannot be negative.");

        _position = newPosition;
        return _position;
    }

    /// <summary>
    /// Creates a clone with independent position state but shared memory mapping.
    /// Safe for concurrent use from multiple threads.
    /// </summary>
    public override object Clone() => new FMountedArchive(this);

    public override void Close()
    {
        if (_ownsFile)
        {
            _accessor.Dispose();
            _mappedFile.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsFile)
        {
            _accessor.Dispose();
            _mappedFile.Dispose();
        }
    }
}

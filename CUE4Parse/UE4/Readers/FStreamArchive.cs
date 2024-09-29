using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Versions;

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

    public override void Close() => _baseStream.Close();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
    {
        CheckReadSize(length);
        var result = new byte[length];
        _baseStream.Read(result, 0, length);
        return result;
    }

    public override object Clone()
    {
        return _baseStream switch
        {
            ICloneable cloneable => new FStreamArchive(Name, (Stream) cloneable.Clone(), Versions) {Position = Position},
            FileStream fileStream => new FStreamArchive(Name, File.Open(fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Versions) {Position = Position},
            _ => new FStreamArchive(Name, _baseStream, Versions) {Position = Position}
        };
    }
}

public class FRandomAccessStreamArchive : FArchive
{
    private readonly IRandomAccessStream _baseStream;

    public FRandomAccessStreamArchive(string name, IRandomAccessStream baseStream, VersionContainer? versions = null) : base(versions)
    {
        _baseStream = baseStream;
        Name = name;
    }

    public override void Close() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    public override int ReadAt(long position, byte[] buffer, int offset, int count)
        => _baseStream.ReadAt(position, buffer, offset, count);

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _baseStream.ReadAtAsync(position, buffer, offset, count, cancellationToken);

    public override Task<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken)
        => _baseStream.ReadAtAsync(position, memory, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] ReadBytes(int length)
    {
        CheckReadSize(length);
        var result = new byte[length];
        _baseStream.Read(result, 0, length);
        return result;
    }

    public override object Clone() => new FRandomAccessStreamArchive(Name, _baseStream, Versions) {Position = Position};
}

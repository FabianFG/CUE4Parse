using System;
using System.IO;

namespace CUE4Parse.Utils;

internal sealed class SegmentFileStream : Stream
{
    private readonly FileStream _fs;
    private readonly long _offset;
    private long _position;

    public SegmentFileStream(string path, long offset, long length)
    {
        if (offset >= 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
            _offset = offset;
            Length = length;
            _position = 0;

            _fs.Seek(_offset, SeekOrigin.Begin);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();

        if (_position >= Length) return 0;

        var max = (int) Math.Min(count, Length - _position);
        var absPos = _offset + _position;

        if (_fs.Position != absPos)
            _fs.Seek(absPos, SeekOrigin.Begin);

        var read = _fs.Read(buffer, offset, max);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (target < 0) throw new IOException("Seek before beginning of the segment");
        if (target > Length) throw new IOException("Seek beyond end of the segment");

        _position = target;
        _fs.Seek(_offset + _position, SeekOrigin.Begin);
        return _position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fs.Dispose();
        }

        base.Dispose(disposing);
    }
}
using System;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Readers;

public class SpliceStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _start;
    private readonly long _length;
    private long _position;

    public SpliceStream(Stream innerStream, long start, long length)
    {
        _innerStream = innerStream;
        _start = start;
        _length = length;
        _position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position { get => _position; set => _position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length)
            return 0;
        _innerStream.Position = _start + _position;
        int toRead = (int) Math.Min(count, _length - _position);
        int read = _innerStream.Read(buffer, offset, toRead);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = offset;
                break;
            case SeekOrigin.Current:
                _position += offset;
                break;
            case SeekOrigin.End:
                _position = _length + offset;
                break;
        }
        return _position;
    }

    public override void Flush() => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}

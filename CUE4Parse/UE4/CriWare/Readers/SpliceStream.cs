using System;
using System.IO;

public class SpliceStream : Stream
{
    private readonly Stream innerStream;
    private readonly long start;
    private readonly long length;
    private long position;

    public SpliceStream(Stream innerStream, long start, long length)
    {
        this.innerStream = innerStream;
        this.start = start;
        this.length = length;
        this.position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => position; set => position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (position >= length)
            return 0;
        innerStream.Position = start + position;
        int toRead = (int) Math.Min(count, length - position);
        int read = innerStream.Read(buffer, offset, toRead);
        position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                position = offset;
                break;
            case SeekOrigin.Current:
                position += offset;
                break;
            case SeekOrigin.End:
                position = length + offset;
                break;
        }
        return position;
    }

    public override void Flush() => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}

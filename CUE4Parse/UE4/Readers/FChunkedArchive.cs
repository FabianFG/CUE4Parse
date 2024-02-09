using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Readers;

public abstract class FChunkedArchive : FArchive {
    private const int BUFFER_SIZE = 1024 * 1024 * 128; // 128 MiB.

    protected FChunkedArchive(string path, VersionContainer versions) : base(versions) {
        Name = path;
        Buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
    }

    protected abstract Span<byte> ReadChunks(long offset, long size);

    public override int Read(byte[] buffer, int offset, int count) {
        if (Position < 0) {
            return 0;
        }

        var n = (int) (Length - Position);
        if (n > count) n = count;
        if (n <= 0)
            return 0;

        Span<byte> data;
        if (n < BUFFER_SIZE) {
            var bufferRangeStart = BufferOffset;
            var bufferRangeEnd = BufferOffset + BUFFER_SIZE;
            if (!(bufferRangeStart <= Position && Position <= bufferRangeEnd)) {
                BufferOffset = Position;
                var blockSize = Math.Min(BUFFER_SIZE, Length - Position).Align(BUFFER_SIZE);
                if (Position.Align(BUFFER_SIZE) != Position) {
                    BufferOffset = Position.Align(BUFFER_SIZE) - BUFFER_SIZE;
                }

                ReadChunks(BufferOffset, blockSize).CopyTo(Buffer);
            }

            data = Buffer.AsSpan().Slice((int)(Position - BufferOffset), n);
        } else {
            data = ReadChunks(Position, n);
        }

        data.CopyTo(buffer.AsSpan(offset));
        Position += n;
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
                   {
                       SeekOrigin.Begin   => offset,
                       SeekOrigin.Current => Position + offset,
                       SeekOrigin.End     => Length + offset,
                       _                  => throw new ArgumentOutOfRangeException()
                   };
        return Position;
    }

    public override bool CanSeek => true;
    public override long Position { get; set; }
    public override string Name { get; }
    private byte[] Buffer { get; }
    private long BufferOffset { get; set; } = -BUFFER_SIZE - 1;

    protected void ImportBuffer(FChunkedArchive other) {
        BufferOffset = other.BufferOffset;
        other.Buffer.AsSpan().CopyTo(Buffer.AsSpan());
    }

    public override void Close() {
        base.Close();
        ArrayPool<byte>.Shared.Return(Buffer);
    }
}

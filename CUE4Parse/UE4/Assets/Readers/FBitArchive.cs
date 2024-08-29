using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Readers;

public class FBitArchive : FArchive
{
    public override bool CanSeek => true;
    public override long Length { get; }
    public override long Position { get; set; }
    public override string Name { get; }

    private readonly byte[] _data;
    private long _bitIndex;

    public FBitArchive(string name, byte[] data) : base()
    {
        _data = data;
        Name = name;
        Length = _data.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(uint length)
    {
        if (length == 0) return 0;
        ulong value;
        Position = _bitIndex >> 3;
        if (Position + 8 > Length)
        {
            var bytes = new byte[8];
            Buffer.BlockCopy(_data, (int)Position, bytes, 0 , (int)(Length - Position));
            value = BitConverter.ToUInt64(bytes);
        }
        else
        {
            value = Unsafe.ReadUnaligned<ulong>(ref _data[Position]);
        }

        var mask = (ulong)(1 << (int)length) - 1;
        var shift = _bitIndex - (Position << 3);
        value = (value >> (int)shift) & mask;
        _bitIndex += length;
        return (int)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FIntVector ReadIntVector(TIntVector3<uint> bits)
    {
        return new FIntVector(Read(bits.X), Read(bits.Y), Read(bits.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        Position = _bitIndex >> 3;
        int n = (int) (Length - Position);
        if (n > count) n = count;
        if (n <= 0) return 0;

        Buffer.BlockCopy(_data, (int) Position, buffer, offset, n);
        Position += n;
        _bitIndex = Position << 3;
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public override object Clone() => new FBitArchive(Name, _data) {Position = Position};
}

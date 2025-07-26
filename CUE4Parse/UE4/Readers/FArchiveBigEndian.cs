using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Readers;

// Thank you RedHaze
public class FArchiveBigEndian : FArchive
{
    private FArchive _baseArchive;

    public FArchiveBigEndian(FArchive baseArchive)
    {
        _baseArchive = baseArchive;
    }

    private static readonly Dictionary<Type, Func<FArchiveBigEndian, object>> _read = new()
    {
        { typeof(short),  ar => BinaryPrimitives.ReadInt16BigEndian(ar.ReadBytes(sizeof(short))) },
        { typeof(int),    ar => BinaryPrimitives.ReadInt32BigEndian(ar.ReadBytes(sizeof(int))) },
        { typeof(long),   ar => BinaryPrimitives.ReadInt64BigEndian(ar.ReadBytes(sizeof(long))) },
        { typeof(ushort), ar => BinaryPrimitives.ReadUInt16BigEndian(ar.ReadBytes(sizeof(ushort))) },
        { typeof(uint),   ar => BinaryPrimitives.ReadUInt32BigEndian(ar.ReadBytes(sizeof(uint))) },
        { typeof(ulong),  ar => BinaryPrimitives.ReadUInt64BigEndian(ar.ReadBytes(sizeof(ulong))) },
        { typeof(float),  ar => BinaryPrimitives.ReadSingleBigEndian(ar.ReadBytes(sizeof(float))) },
        { typeof(double), ar => BinaryPrimitives.ReadDoubleBigEndian(ar.ReadBytes(sizeof(double))) },
    };

    public override int Read(byte[] buffer, int offset, int count) => _baseArchive.Read(buffer, offset, count);
    public override string ReadString() => Encoding.UTF8.GetString(ReadArray<byte>());
    public override long Seek(long offset, SeekOrigin origin) => _baseArchive.Seek(offset, origin);

    public sealed override T Read<T>()
    {
        if (_read.TryGetValue(typeof(T), out var func))
            return (T)func(this);

        return base.Read<T>();
    }

    public override T[] ReadArray<T>(int length)
    {
        var size = Unsafe.SizeOf<T>();
        var readLength = size * length;
        CheckReadSize(readLength);

        var buffer = ReadBytes(readLength);
        if (size == 1)
        {
        }
        else if (size == 2)
        {
            ReverseEndian(buffer.AsSpan().Cast<byte, ushort>());
        }
        else if (typeof(T) == typeof(uint) || typeof(T) == typeof(int) || typeof(T) == typeof(float))
        {
            ReverseEndian(buffer.AsSpan().Cast<byte, uint>());
        }
        else if (typeof(T) == typeof(ulong) || typeof(T) == typeof(long) || typeof(T) == typeof(double))
        {
            ReverseEndian(buffer.AsSpan().Cast<byte, ulong>());
        }
        else
        {
            throw new ParserException("Unsupported type for ReadArray: " + typeof(T).Name);
        }
        var result = new T[length];
        if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref buffer[0], (uint)(readLength));
        return result;
    }

    static void ReverseEndian<TSwap>(Span<TSwap> span) where TSwap : unmanaged
    {
        if (typeof(TSwap) == typeof(ushort))
        {
            var span2 = span.Cast<TSwap, ushort>();
            for (int i = 0; i < span.Length; i++)
            {
                span2[i] = BinaryPrimitives.ReverseEndianness(span2[i]);
            }
        }
        else if (typeof(TSwap) == typeof(uint))
        {
            var span4 = span.Cast<TSwap, uint>();
            for (int i = 0; i < span.Length; i++)
            {
                span4[i] = BinaryPrimitives.ReverseEndianness(span4[i]);
            }
        }
        else if (typeof(TSwap) == typeof(ulong))
        {
            var span8 = span.Cast<TSwap, ulong>();
            for (int i = 0; i < span.Length; i++)
            {
                span8[i] = BinaryPrimitives.ReverseEndianness(span8[i]);
            }
        }
    }

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name => _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override object Clone() => new FArchiveBigEndian(_baseArchive);
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    public override T Read<T>()
    {
        if (_read.TryGetValue(typeof(T), out var func))
            return (T)func(this);

        return base.Read<T>();
    }

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name => _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override object Clone()
    {
        throw new System.NotImplementedException();
    }
}

using System;
using System.Buffers.Binary;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Readers.Common;

public class BinaryReaderEndian(Stream input) : BinaryReader(input)
{
    public ushort ReadUInt16BE() => BinaryPrimitives.ReverseEndianness(base.ReadUInt16());

    public short ReadInt16BE() => (short)ReadUInt16BE();

    public uint ReadUInt32BE() => BinaryPrimitives.ReverseEndianness(base.ReadUInt32());

    public int ReadInt32BE() => (int)ReadUInt32BE();

    public ulong ReadUInt64BE() => BinaryPrimitives.ReverseEndianness(base.ReadUInt64());

    public long ReadInt64BE() => (long) ReadUInt64BE();

    public float ReadSingleBE()
    {
        Span<byte> bytes = stackalloc byte[4];
        base.Read(bytes);
        return BinaryPrimitives.ReadSingleBigEndian(bytes);
    }
}

using System;
using System.Buffers.Binary;
using System.IO;

namespace CUE4Parse.UE4.Writers;

public class FArchiveWriter : BinaryWriter
{
    private readonly MemoryStream _memoryData;
    private readonly bool bUseBigEndian;

    public FArchiveWriter(bool useBigEndian = false)
    {
        _memoryData = new MemoryStream {Position = 0};
        OutStream = _memoryData;
            
        bUseBigEndian = useBigEndian;
    }

    public byte[] GetBuffer() => _memoryData.ToArray();

    public long Length => _memoryData.Length;
    public long Position => _memoryData.Position;

    public override void Write(short value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }

    public override void Write(ushort value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }

    public override void Write(int value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }

    public override void Write(uint value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }

    public override void Write(long value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }

    public override void Write(ulong value)
    {
        if (bUseBigEndian) value = BinaryPrimitives.ReverseEndianness(value);
        base.Write(value);
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _memoryData.Dispose();
    }
}
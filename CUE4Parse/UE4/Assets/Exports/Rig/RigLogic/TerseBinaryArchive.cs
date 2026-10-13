using System.Buffers.Binary;

namespace CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;

public sealed class TerseBinaryArchive
{
    private readonly byte[] _data;
    public long Position { get; set; }
    public long Length { get; }
    public long Remaining => Length - Position;

    public TerseBinaryArchive(byte[] data, long start = 0, long? length = null)
    {
        _data = data;
        Position = start;
        Length = start + (length ?? (data.Length - start));
    }

    public bool PeekMagicBe(uint expected)
    {
        if (Remaining < 4)
            return false;
        var value = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan((int) Position, 4));
        return value == expected;
    }

    public byte ReadUInt8()
    {
        Ensure(1);
        return _data[Position++];
    }

    public bool ReadBool8() => ReadUInt8() != 0;

    public ushort ReadUInt16()
    {
        Ensure(2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan((int) Position, 2));
        Position += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        Ensure(4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan((int) Position, 4));
        Position += 4;
        return value;
    }

    public ulong ReadUInt64()
    {
        Ensure(8);
        var value = BinaryPrimitives.ReadUInt64BigEndian(_data.AsSpan((int) Position, 8));
        Position += 8;
        return value;
    }

    public float ReadFloat()
    {
        Ensure(4);
        var bits = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan((int) Position, 4));
        Position += 4;
        return BitConverter.Int32BitsToSingle(unchecked((int) bits));
    }

    public uint ReadCount()
    {
        var count = ReadUInt32();
        if (count > Remaining)
            throw new InvalidDataException($"Terse count {count} exceeds remaining {Remaining} bytes");
        return count;
    }

    public T[] ReadArray<T>(Func<T> read)
    {
        var count = ReadCount();
        var items = new T[count];
        for (var i = 0; i < count; i++)
            items[i] = read();
        return items;
    }

    public T[][] ReadMatrix<T>(Func<T> read)
    {
        var count = ReadCount();
        var items = new T[count][];
        for (var i = 0; i < count; i++)
            items[i] = ReadArray(read);
        return items;
    }

    public ushort[] ReadUInt16Array()
    {
        var count = (int) ReadCount();
        return ReadUInt16Values(count);
    }

    public ushort[] ReadUInt16Values(int count)
    {
        Ensure(count * 2);
        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = ReadUInt16();
        return result;
    }

    public float[] ReadFloatArray()
    {
        var count = (int) ReadCount();
        var result = new float[count];
        for (var i = 0; i < count; i++)
            result[i] = ReadFloat();
        return result;
    }

    public void EnsureConsumed()
    {
        if (Remaining != 0)
            throw new InvalidDataException($"Snapshot has {Remaining} unconsumed bytes");
    }

    private void Ensure(int count)
    {
        if (Remaining < count)
            throw new EndOfStreamException($"Need {count} bytes, {Remaining} remaining");
    }
}

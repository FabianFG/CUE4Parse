using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CUE4Parse.UE4.Writers;

public class FArchiveWriter : BinaryWriter
{
    private readonly MemoryStream _memoryData;

    public FArchiveWriter()
    {
        _memoryData = new MemoryStream { Position = 0 };
        OutStream = _memoryData;
    }

    public byte[] GetBuffer() => _memoryData.ToArray();

    public long Length => _memoryData.Length;
    public long Position => _memoryData.Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ISerializable item)
    {
        item.Serialize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeEnumerable<T>(IEnumerable<T> items) where T : ISerializable
    {
        SerializeEnumerable(items, item => Serialize(item));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeEnumerable<T>(IEnumerable<T> items, Action<T> writeElement)
    {
        var itms = items.ToList();
        SerializeEnumerable(itms, itms.Count, writeElement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeEnumerable<T>(IEnumerable<T>? items, int count, Action<T> writeElement)
    {
        if (items == null)
        {
            Write(-1);
        }
        else
        {
            var itms = items.ToList();

            Write(count);
            for (var idx = 0; idx < count; idx++)
            {
                writeElement(itms[idx]);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SerializeDictionary<TKey, TValue>(IDictionary<TKey, TValue>? items, Action<TKey> writeKey, Action<TValue> writeValue) where TKey : notnull
    {
        if (items == null)
        {
            Write(-1);
        }
        else
        {
            Write(items.Count);
            foreach (var item in items)
            {
                writeKey(item.Key);
                writeValue(item.Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(string value, int length)
    {
        var padded = new byte[length];
        var bytes = Encoding.UTF8.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        Write(padded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize<T>(object? arg)
    {
        var type = typeof(T);

        if (!type.IsPrimitive || arg == null) return;

        if (type == typeof(int))
        {
            Write((int) arg);
        }
        else if (type == typeof(float))
        {
            Write((float) arg);
        }
        else if (type == typeof(double))
        {
            Write((double) arg);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _memoryData.Dispose();
    }
}

public interface ISerializable
{
    void Serialize(FArchiveWriter Ar);
}
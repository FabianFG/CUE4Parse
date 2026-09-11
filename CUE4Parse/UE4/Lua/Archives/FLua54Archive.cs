using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Lua.Archives;

public class FLua54Archive(string name, byte[] data, VersionContainer? versions = null) : FByteArchive(name, data, versions)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual ulong ReadLuaInt()
    {
        ulong v = 0;
        while (true)
        {
            int b = Read<byte>();
            v = (v << 7) | (uint) (b & 0x7F);

            if ((b & 0x80) != 0)
                break;
        }

        return v;
    }

    public virtual string ReadLuaString()
    {
        ulong size = ReadLuaInt();
        if (size <= 1)
            return string.Empty;

        int length = (int) size - 1;
        byte[] buffer = ReadBytes(length);

        return Encoding.UTF8.GetString(buffer);
    }

    public virtual T[] ReadLuaArray<T>(Func<T> readElement)
    {
        int size = (int) ReadLuaInt();
        if (size <= 0)
            return [];

        T[] array = new T[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = readElement();
        }

        return array;
    }
}

public class FLua54ArchiveWriter : FArchiveWriter
{
    public void WriteLuaInt(ulong value)
    {
        if (value == 0)
        {
            Write((byte) 0x80);
            return;
        }

        // Just so it's clear, max bytes needed to encode a 64-bit value using 7 bits per byte (it's 10 bytes)
        const int MaxLuaIntBytes = (sizeof(ulong) * 8 + 6) / 7;
        Span<byte> buffer = stackalloc byte[MaxLuaIntBytes];
        var index = buffer.Length;

        var first = true;
        while (value > 0 || first)
        {
            var b = (byte) (value & 0x7F);

            if (first)
                b |= 0x80;

            buffer[--index] = b;

            value >>= 7;
            first = false;
        }

        Write(buffer[index..]);
    }

    public void WriteLuaString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteLuaInt(0);
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(value);

        WriteLuaInt((ulong) buffer.Length + 1);
        Write(buffer);
    }

    public void WriteLuaArray<T>(T[]? array, Action<T> writeElement)
    {
        if (array == null)
        {
            WriteLuaInt(0);
            return;
        }

        WriteLuaInt((ulong) array.Length);
        foreach (var item in array)
        {
            writeElement(item);
        }
    }
}

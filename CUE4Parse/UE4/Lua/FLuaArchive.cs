using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Lua;

public class FLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FByteArchive(name, data, versions)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadLuaInt()
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

    public T[] ReadLuaArray<T>(Func<T> readElement)
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

public class FLuaArchiveWriter(Stream stream) : BinaryWriter(stream)
{
    public void WriteLuaInt(ulong v)
    {
        if (v == 0)
        {
            Write((byte) 0x80);
            return;
        }

        var bytes = new List<byte>();
        bool first = true;
        while (v > 0 || first)
        {
            byte x = (byte) (v & 0x7F);
            if (first)
                x |= 0x80;

            bytes.Add(x);
            v >>= 7;
            first = false;
        }
        bytes.Reverse();
        Write(bytes.ToArray());
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

    public void WriteLuaArray<T>(T[] array, Action<T> writeElement)
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

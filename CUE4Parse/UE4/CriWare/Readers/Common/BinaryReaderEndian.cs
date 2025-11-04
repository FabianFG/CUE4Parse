using System;
using System.IO;

namespace CUE4Parse.UE4.CriWare.Readers.Common;

public class BinaryReaderEndian(Stream input) : BinaryReader(input)
{
    public ushort ReadUInt16BE()
    {
        ushort le = base.ReadUInt16();
        return (ushort)(((le & 0x00FF) << 8) | ((le & 0xFF00) >> 8));
    }

    public short ReadInt16BE() => (short)ReadUInt16BE();

    public uint ReadUInt32BE()
    {
        uint le = base.ReadUInt32();
        return ((le & 0x000000FF) << 24)
            | ((le & 0x0000FF00) << 8)
            | ((le & 0x00FF0000) >> 8)
            | ((le & 0xFF000000) >> 24);
    }

    public int ReadInt32BE() => (int)ReadUInt32BE();

    public ulong ReadUInt64BE()
    {
        ulong le = base.ReadUInt64();
        return ((le & 0x00000000000000FF) << 56)
            | ((le & 0x000000000000FF00) << 40)
            | ((le & 0x0000000000FF0000) << 24)
            | ((le & 0x00000000FF000000) << 8)
            | ((le & 0x000000FF00000000) >> 8)
            | ((le & 0x0000FF0000000000) >> 24)
            | ((le & 0x00FF000000000000) >> 40)
            | ((le & 0xFF00000000000000) >> 56);
    }

    public long ReadInt64BE()
    {
        return (long)ReadUInt64BE();
    }

    public float ReadSingleBE()
    {
        float le = base.ReadSingle();
        byte[] floatBytes = BitConverter.GetBytes(le);
        byte[] reversed = new byte[4];

        for (int i = 0; i < 4; i++)
        {
            reversed[i] = floatBytes[3 - i];
        }

        return BitConverter.ToSingle(reversed, 0);
    }
}

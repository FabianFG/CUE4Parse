using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Lua.Archives;

public class FLua53Archive(string name, byte[] data, VersionContainer? versions = null) : FByteArchive(name, data, versions)
{
    public virtual string ReadLuaString()
    {
        byte sizeByte = Read<byte>();
        if (sizeByte == 0)
            return string.Empty;

        int size = sizeByte == 0xFF ? Read<int>() : sizeByte;
        int length = size - 1;
        if (length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(ReadBytes(length));
    }
}

public class FLua53ArchiveWriter(Stream stream) : BinaryWriter(stream)
{
    public virtual void WriteLuaString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Write((byte) 0x00);
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(value);
        int size = buffer.Length + 1;

        if (size < 0xFF)
        {
            Write((byte) size);
        }
        else
        {
            Write((byte) 0xFF);
            Write(size);
        }

        Write(buffer);
    }
}

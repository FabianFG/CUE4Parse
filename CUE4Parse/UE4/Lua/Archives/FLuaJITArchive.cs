using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Lua.Archives;

public class FLuaJITArchive(string name, byte[] data, VersionContainer? versions = null) : FByteArchive(name, data, versions)
{
    // https://github.com/LuaJIT/LuaJIT/blob/a2bde60819d83e6f75130ac2c93ee4b3c7615800/src/lj_buf.c#L291
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadUleb128()
    {
        var value = 0;
        var shift = 0;

        byte b;
        do
        {
            b = Read<byte>();
            value |= (b & 0x7F) << shift;
            shift += 7;
        } while (b >= 0x80);

        return value;
    }

    public string ReadLuaString()
    {
        var length = ReadUleb128();
        if (length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(ReadBytes(length));
    }
}

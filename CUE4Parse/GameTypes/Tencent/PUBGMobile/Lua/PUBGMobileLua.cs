using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.Lua;

public class PUBGMobileLua
{
    //public class FPUBGMobileLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLua53Archive(name, data, versions)
    //{
    //    // Strings are encrypted
    //    public override string ReadLuaString()
    //    {
    //        var sizeByte = Read<byte>();
    //        if (sizeByte == 0)
    //            return string.Empty;

    //        var size = sizeByte == 0xFF ? Read<int>() : sizeByte;
    //        var length = size - 1;

    //        if (length <= 0)
    //            return string.Empty;

    //        var buffer = ReadBytes(length);
    //        for (int i = 0; i < length; i++)
    //        {
    //            buffer[i] ^= _stringKey[i % _stringKey.Length];
    //        }

    //        return Encoding.UTF8.GetString(buffer);
    //    }
    //}

    // TODO
    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData)
    {
        return encryptedData;
        //var Ar = new FPUBGMobileLuaArchive(name, encryptedData);
        //using var msOut = new MemoryStream();
        //using var writer = new FLua53ArchiveWriter(msOut);
        //FLuaWriter53.Write(writer, FLua53Reader.ReadLuaBytecode(Ar));
        //writer.Flush();

        //return msOut.ToArray();
    }
}

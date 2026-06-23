using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;

namespace CUE4Parse.GameTypes.NFS.Mobile.Lua;

public class NFSLua
{
    public static byte[] RestoreLuaBytecode(string name, byte[] encryptedData)
    {
        using var Ar = new FLua54Archive(name, encryptedData);
        using var msOut = new MemoryStream();
        using var writer = new FLua54ArchiveWriter(msOut);

        FLuaWriter54.Write(writer, new LuaBytecode
        {
            Header = ReadHeader(Ar),
            MainFunc = FLua54Reader.ReadFunction(Ar)
        });

        writer.Flush();
        return msOut.ToArray();
    }

    private static LuaHeader ReadHeader(FLua54Archive Ar)
    {
        Ar.Position += 31;
        return new LuaHeader
        {
            Signature = FLuaReader.LUA_SIGNATURE,
            Version = 0x54,
            Format = FLuaReader.LUAC_FORMAT,
            LuacData = FLuaReader.LUAC_DATA,
            InstructionSize = 4,
            IntegerSize = 8,
            NumberSize = 8,
            LuacInt = FLuaReader.LUAC_INT,
            LuacNum = FLuaReader.LUAC_NUM,
            Closure = 1
        };
    }
}

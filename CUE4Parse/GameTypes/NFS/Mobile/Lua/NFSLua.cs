using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;

namespace CUE4Parse.GameTypes.NFS.Mobile.Lua;

public class NFSLua
{
    public static byte[] RestoreLuaBytecode(string name, byte[] encryptedData)
    {
        using var Ar = new FLua54Archive(name, encryptedData);
        var lua = new LuaBytecode
        {
            Header = ReadHeader(Ar),
            MainFunc = FLua54Reader.ReadFunction(Ar)
        };

        return new FLuaWriter54(lua).GetBuffer();
    }

    private static LuaHeader ReadHeader(FLua54Archive Ar)
    {
        Ar.Position += 31;
        return FLua54Reader.DefaultHeader;
    }
}

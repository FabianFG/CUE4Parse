using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.ABI.UE4.Lua;

public class FABILua54Archive(string name, byte[] data, bool isMobile, VersionContainer? versions = null) : FLua54Archive(name, data, versions)
{
    private static readonly byte[] _desktopKey = Encoding.ASCII.GetBytes("hotbeaf\0");
    private static readonly byte[] _mobileKey = Encoding.ASCII.GetBytes("leaftenc"); // "leaftencent" truncated

    private readonly byte[] _key = isMobile ? _mobileKey : _desktopKey;

    public override T Read<T>()
    {
        if (typeof(T) == typeof(byte))
        {
            byte raw = base.Read<byte>();
            byte dec = (byte) (raw ^ _key[0]);
            return (T) (object) dec;
        }

        return base.Read<T>();
    }

    public override byte[] ReadBytes(int length)
    {
        var enc = base.ReadBytes(length);
        var dec = new byte[enc.Length];
        var prev = _key[0];
        var mask = _key.Length;
        for (int i = 0; i < enc.Length; i++)
        {
            var k = (byte) ((_key[i % mask] | prev) & 0xFF);
            var plain = (byte) (enc[i] ^ k);
            dec[i] = plain;
            prev = plain;
        }

        return dec;
    }

    public override ulong ReadLuaInt()
    {
        ulong v = 0;
        while (true)
        {
            var b = base.Read<byte>(); // Not encrypted

            v = (v << 7) | (uint) (b & 0x7F);

            if ((b & 0x80) != 0)
                break;
        }

        return v;
    }
}

public static class ABILuaReader
{
    private const byte LuaVersion = 0x54;
    private static readonly byte[] _opcodeTable =
    [
        8, 3, 13, 74, 81, 76, 12, 67, 5, 7, 0, 1, 9, 14, 68, 20,
        18, 69, 15, 17, 75, 80, 79, 56, 57, 58, 59, 60, 61, 62, 63, 64,
        65, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 21, 22, 23, 24, 25, 26, 27, 28, 29,
        30, 31, 32, 33, 16, 6, 11, 66, 2, 10, 73, 19, 70, 4, 77, 72,
        71, 78, 82
    ];
    private static readonly Dictionary<byte, byte> _opcodeMapping =
        _opcodeTable.Select((mapped, opcode) => (opcode, mapped)).ToDictionary(x => (byte) x.opcode, x => x.mapped);

    public static byte[] DecryptLuaBytecode(byte[] bytes, bool isMobile)
    {
        var Ar = new FABILua54Archive("ABILua", bytes, isMobile);

        using var msOut = new MemoryStream();
        using var writer = new FLua54ArchiveWriter(msOut);
        FLuaWriter54.Write(writer, ReadLuaBytecode(Ar));

        writer.Flush();
        return msOut.ToArray();
    }

    private static LuaBytecode ReadLuaBytecode(FABILua54Archive Ar) => new()
    {
        Header = ReadHeader(Ar),
        MainFunc = FLua54Reader.ReadFunction(Ar, _opcodeMapping)
    };

    private static LuaHeader ReadHeader(FABILua54Archive Ar)
    {
        Ar.ReadBytes(4); // Custom magic
        Ar.Read<byte>(); // Custom version
        var format = Ar.Read<byte>();
        Ar.ReadBytes(6); // Obfuscated luacData
        var instructionSize = Ar.Read<byte>();
        var integerSize = Ar.Read<byte>();
        var numberSize = Ar.Read<byte>();
        Ar.ReadBytes(8); // Obfuscated luacInt
        Ar.ReadBytes(8); // Obfuscated luacNum
        var closure = Ar.Read<byte>();

        return new LuaHeader
        {
            Signature = FLuaReader.LUA_SIGNATURE,
            Version = LuaVersion,
            Format = format,
            LuacData = FLuaReader.LUAC_DATA,
            InstructionSize = instructionSize,
            IntegerSize = integerSize,
            NumberSize = numberSize,
            LuacInt = FLuaReader.LUAC_INT,
            LuacNum = FLuaReader.LUAC_NUM,
            Closure = closure
        };
    }
}

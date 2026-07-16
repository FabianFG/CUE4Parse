using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.UDWN.Lua;

public class FUndawnLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLua53Archive(name, data, versions)
{
    private readonly byte[] _stringKey =
    [
        0x7A, 0x81, 0xF4, 0xF5, 0xCA, 0xDF, 0x15, 0xBD,
        0x0A, 0x1C, 0x0D, 0xDB, 0xFC, 0x59, 0x34, 0xAB,
        0x2D, 0x90, 0x1F, 0x11, 0x1D, 0x67, 0xC2, 0x7D,
        0x1C, 0x7F, 0x81, 0xE7, 0x92, 0xB4, 0x47, 0xD7
    ];

    // Strings are encrypted
    public override string ReadLuaString()
    {
        var sizeByte = Read<byte>();
        if (sizeByte == 0)
            return string.Empty;

        var size = sizeByte == 0xFF ? Read<int>() : sizeByte;
        var length = size - 1;

        if (length <= 0)
            return string.Empty;

        var buffer = ReadBytes(length);
        for (int i = 0; i < length; i++)
        {
            buffer[i] ^= _stringKey[i % _stringKey.Length];
        }

        return Encoding.UTF8.GetString(buffer);
    }
}

public class UndawnLua
{
    private const byte XorKey = 0x15;
    // Mapped based on luaV_execute (0x00000001404909B0)
    private static readonly Dictionary<byte, byte> _opcodeMapping = new()
    {
        { 0, 6 },   // gettabup
        { 1, 35 },  // testset
        { 2, 36 },  // call
        { 3, 31 },  // eq
        { 4, 32 },  // lt
        { 5, 33 },  // le
        { 6, 42 },  // tforloop
        { 7, 10 },  // settable
        { 8, 37 },  // tailcall
        { 9, 43 },  // setlist
        { 10, 40 }, // forprep
        { 11, 12 }, // self
        { 12, 0 },  // move
        { 13, 44 }, // closure
        { 14, 2 },  // loadkx
        { 15, 38 }, // return
        { 16, 13 }, // add
        { 17, 14 }, // sub
        { 18, 15 }, // mul
        { 19, 16 }, // mod
        { 20, 17 }, // pow
        { 21, 18 }, // div
        { 22, 19 }, // idiv
        { 23, 20 }, // band
        { 24, 21 }, // bor
        { 25, 22 }, // bxor
        { 26, 23 }, // shl
        { 27, 24 }, // shr
        { 28, 25 }, // unm
        { 29, 26 }, // bnot
        { 30, 27 }, // not
        { 31, 28 }, // len
        { 32, 8 },  // settabup
        { 33, 45 }, // vararg
        { 34, 4 },  // loadnil
        { 35, 11 }, // newtable
        { 36, 34 }, // test
        { 37, 3 },  // loadbool
        { 38, 41 }, // tforcall
        { 39, 39 }, // forloop
        { 40, 1 },  // loadk
        { 41, 7 },  // gettable
        { 42, 29 }, // concat
        { 43, 9 },  // setupval
        { 44, 30 }, // jmp
        { 45, 5 },  // getupval
    };

    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData)
    {
        if (!FLuaReader.IsValidLuaMagic(encryptedData))
            throw new InvalidDataException("Failed to decrypt. Expected Lua magic");

        for (int i = 4; i < encryptedData.Length; i++)
            encryptedData[i] ^= XorKey; // Part of the header isn't encrypted but I don't care

        using var Ar = new FUndawnLuaArchive(name, encryptedData, null);
        var lua = new LuaBytecode
        {
            Header = ReadHeader(Ar),
            SizeUpvalues = Ar.Read<byte>(),
            MainFunc = FLua53Reader.ReadFunction(Ar, null, _opcodeMapping),
        };

        using var ms = new MemoryStream();
        using var writer = new FLua53ArchiveWriter(ms);

        FLuaWriter53.Write(writer, lua);
        writer.Flush();

        return ms.ToArray();
    }

    private static LuaHeader ReadHeader(FLua53Archive Ar)
    {
        var header = new LuaHeader();

        header.Signature = Ar.ReadBytes(4);
        header.Version = Ar.Read<byte>();
        Ar.Position += 1;
        header.Format = FLuaReader.LUAC_FORMAT;
        Ar.Position += 6;
        header.LuacData = FLuaReader.LUAC_DATA;
        header.CintSize = Ar.Read<byte>();
        header.SizeTSize = Ar.Read<byte>();
        Ar.Position += 1;
        header.InstructionSize = 4;
        header.IntegerSize = Ar.Read<byte>();
        header.NumberSize = 8;
        Ar.Position += 16;
        header.LuacInt = FLuaReader.LUAC_INT;
        header.LuacNum = FLuaReader.LUAC_NUM;

        return header;
    }
}

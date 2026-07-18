using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.Lua;

public static class PUBGMobileLua
{
    public class FPUBGMobileLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLua53Archive(name, data, versions)
    {
        private static readonly byte[] _stringKey =
        [
            0x11, 0x21, 0x36, 0x47, 0x46, 0x57, 0xA7, 0x8D,
            0x9D, 0x84, 0x90, 0xD8, 0xAB, 0x00, 0x8C, 0x35,
            0x26, 0x1A, 0xF7, 0xE4, 0x58, 0x05, 0xB8, 0xB3,
            0x15, 0x07, 0xD0, 0x2C, 0x1E, 0x8F, 0xF6, 0xC8
        ];

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
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] ^= _stringKey[i % _stringKey.Length];

            return Encoding.UTF8.GetString(buffer);
        }
    }

    private static readonly Dictionary<byte, byte> _opcodeMapping = new()
    {
        [0] = 13,  // ADD
        [1] = 14,  // SUB
        [2] = 15,  // MUL
        [3] = 16,  // MOD
        [4] = 17,  // POW
        [5] = 18,  // DIV
        [6] = 19,  // IDIV
        [7] = 20,  // BAND
        [8] = 21,  // BOR
        [9] = 22,  // BXOR
        [10] = 23, // SHL
        [11] = 24, // SHR
        [12] = 25, // UNM
        [13] = 26, // BNOT
        [14] = 27, // NOT
        [15] = 28, // LEN
        [16] = 29, // CONCAT
        [17] = 0,  // MOVE
        [18] = 1,  // LOADK
        [19] = 2,  // LOADKX
        [20] = 3,  // LOADBOOL
        [21] = 4,  // LOADNIL
        [22] = 5,  // GETUPVAL
        [23] = 6,  // GETTABUP
        [24] = 7,  // GETTABLE
        [25] = 8,  // SETTABUP
        [26] = 9,  // SETUPVAL
        [27] = 10, // SETTABLE
        [28] = 11, // NEWTABLE
        [29] = 12  // SELF
    };

    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData, EGame game)
    {
        if (!FLuaReader.IsValidLuaMagic(encryptedData))
            return encryptedData;

        if (game is GAME_PUBGLite)
        {
            using var liteAr = new FLua53Archive(name, encryptedData);
            using var msOutLite = new MemoryStream();
            using var writerLite = new FLua53ArchiveWriter(msOutLite);
            FLuaWriter53.Write(writerLite, FLua53Reader.ReadLuaBytecode(liteAr, _opcodeMapping));
            writerLite.Flush();

            return msOutLite.ToArray();
        }

        using var Ar = new FPUBGMobileLuaArchive(name, encryptedData);
        var lua = new LuaBytecode
        {
            Header = FLua53Reader.ReadHeader(Ar),
            SizeUpvalues = Ar.Read<byte>(),
            MainFunc = ReadFunction(Ar, null)
        };

        using var msOut = new MemoryStream();
        using var writer = new FLua53ArchiveWriter(msOut);
        FLuaWriter53.Write(writer, lua);
        writer.Flush();

        return msOut.ToArray();
    }

    private static LuaFunction ReadFunction(FPUBGMobileLuaArchive Ar, string? parentSource)
    {
        var sourceString = Ar.ReadLuaString();
        if (string.IsNullOrEmpty(sourceString))
            sourceString = parentSource ?? string.Empty;

        var func = new LuaFunction
        {
            SourceName = sourceString,
            LineDefined = (ulong) Ar.Read<int>(),
            LastLineDefined = (ulong) Ar.Read<int>(),
            NumParams = Ar.Read<byte>(),
            IsVarArg = Ar.Read<byte>(),
            MaxStackSize = Ar.Read<byte>()
        };

        FLua53Reader.ReadCode(Ar, func, _opcodeMapping);
        FLua53Reader.ReadConstants(Ar, func);
        FLua53Reader.ReadUpvalues(Ar, func);
        func.Protos = Ar.ReadArray(() => ReadFunction(Ar, func.SourceName));
        ReadDebug(Ar, func);

        return func;
    }

    private static void ReadDebug(FPUBGMobileLuaArchive Ar, LuaFunction func)
    {
        var sizeLineInfo = Ar.Read<int>();
        var compressedLineInfo = Ar.ReadBytes(sizeLineInfo);

        var absLineInfo = Ar.ReadArray(() => new LuaAbsLineInfo
        {
            Pc = (ulong) Ar.Read<int>(),
            Line = (ulong) Ar.Read<int>()
        });

        var debug = new LuaDebug
        {
            SizeLineInfo = (ulong) sizeLineInfo,
            LineInfo = ExpandLineInfo(func.LineDefined, compressedLineInfo, absLineInfo),
            AbsLineInfo = absLineInfo,
            LocVars = Ar.ReadArray(() => new LuaLocVar
            {
                NameData = Ar.ReadLuaString(),
                StartPc = (ulong) Ar.Read<int>(),
                EndPc = (ulong) Ar.Read<int>()
            }),
            UpvalueNames = Ar.ReadArray(() => new LuaUpvalueName
            {
                NameData = Ar.ReadLuaString()
            })
        };

        func.Debug = debug;
    }

    private static byte[] ExpandLineInfo(ulong lineDefined, byte[] compressedLineInfo, LuaAbsLineInfo[] absLineInfo)
    {
        var result = new byte[compressedLineInfo.Length * sizeof(int)];
        var line = (int) lineDefined;
        var absIndex = 0;

        for (var pc = 0; pc < compressedLineInfo.Length; pc++)
        {
            var delta = (sbyte) compressedLineInfo[pc];
            line = delta == sbyte.MinValue ? (int) absLineInfo[absIndex++].Line : line + delta;
            BitConverter.GetBytes(line).CopyTo(result, pc * sizeof(int));
        }

        return result;
    }
}

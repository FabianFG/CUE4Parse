using System.IO;
using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.PUBG.UE4.Lua;

public class FGFPLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLua53Archive(name, data, versions)
{
    private readonly byte[] _stringKey =
    [
        0xEF, 0xC1, 0x71, 0x3E, 0xE3, 0x34, 0x7D, 0x24,
        0x58, 0xE1, 0x9A, 0x38, 0x4F, 0xA4, 0x6D, 0x08,
        0x64, 0x70, 0xAC, 0xF2, 0xBC, 0xE6, 0x2E, 0x41,
        0x4F, 0x00, 0x83, 0xE7, 0xE7, 0x0B, 0x20, 0x07
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

public class GameForPeaceLua
{
    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData)
    {
        using var Ar = new FGFPLuaArchive(name, encryptedData, null);

        var lua = ReadBytecode(Ar);

        using var msOut = new MemoryStream();
        using (var writer = new FLua53ArchiveWriter(msOut))
        {
            FLuaWriter53.Write(writer, lua);
            writer.Flush();
        }

        return msOut.ToArray();
    }

    private static LuaBytecode ReadBytecode(FGFPLuaArchive Ar)
    {
        var lua = new LuaBytecode
        {
            Header = ReadHeader(Ar),
            SizeUpvalues = Ar.Read<byte>(),
            MainFunc = ReadFunction(Ar, null)
        };

        return lua;
    }

    private static LuaHeader ReadHeader(FGFPLuaArchive Ar)
    {
        var header = new LuaHeader
        {
            Signature = Ar.ReadBytes(4),
            Version = Ar.Read<byte>(),
            Format = Ar.Read<byte>(),
            LuacData = Ar.ReadBytes(6),
            CintSize = Ar.Read<byte>(),
            SizeTSize = Ar.Read<byte>(),
            InstructionSize = Ar.Read<byte>(),
            IntegerSize = Ar.Read<byte>(),
            NumberSize = Ar.Read<byte>(),
            LuacInt = Ar.ReadBytes(8),
            LuacNum = Ar.ReadBytes(8)
        };

        header.Format = FLuaReader.LUAC_FORMAT; // 1 -> 0 

        return header;
    }

    private static LuaFunction ReadFunction(FGFPLuaArchive Ar, string? parentSource)
    {
        var sourceString = Ar.ReadLuaString();
        if (string.IsNullOrEmpty(sourceString))
            sourceString = parentSource ?? string.Empty;

        var func = new LuaFunction
        {
            SourceName = sourceString,
            LineDefined = Ar.Read<ushort>(), // int -> ushort
            NumParams = Ar.Read<byte>(),
            LastLineDefined = Ar.Read<ushort>() // int -> ushort
        };

        Ar.Position += 1; // ??

        func.IsVarArg = Ar.Read<byte>();
        func.MaxStackSize = Ar.Read<byte>();

        ReadCode(Ar, func);
        ReadConstants(Ar, func);
        ReadUpvalues(Ar, func);
        ReadProtos(Ar, func);
        ReadDebug(Ar, func);

        return func;
    }

    // They use custom opcodes, currently not reconstructed back to standard Lua
    // there's a list (order might be incorrect):
    //
    // - SUPERCODE3_POS3 (custom)
    // - LOADBOOL
    // - LOADK
    // - SETTABLE_CLOSUREorGETTABUP (custom)
    // - SETTABUP
    // - GETTABLE
    // - JMP
    // - GETTABLE_SETTABLEorTEST (custom)
    // - TFORLOOP
    // - LOADK_CALLorGETTABUP (custom)
    // - RETURN
    // - TAILCALL
    // - GETTABUP_SELForVARARG (custom)
    // - VARARG
    // - CUSTOMJMP (custom)
    // - FORPREP
    // - BNOT
    // - NOT
    // - SHR
    // - UNM
    // - BXOR
    // - SHL
    // - BAND
    // - BOR
    // - DIV
    // - IDIV
    // - MOD
    // - POW
    // - SUB
    // - MUL
    // - EQ_JMP (custom)
    // - ADD
    // - CONCAT
    // - NEWTABLE_SETTABLE_SETTABLE (custom)
    // - SETTABLE
    // - LOADKX
    // - SUPERCODE3_POS2 (custom)
    // - FORLOOP
    // - SETLIST
    // - GETTABUP_GETTABLE_CALLorGETTABLE (custom)
    // - CALL
    // - NEWTABLE
    // - SETUPVAL
    // - NEWTABLE_NEWTABLEorSETTABLE (custom)
    // - TESTSET
    // - CLOSURE
    // - LEN
    // - TFORCALL
    // - SELF_LOADKorMOVE (custom)
    // - EXTRAARG
    // - GETTABUP_LOADKorMOVE (custom)
    // - GETTABUP
    // - SELF
    // - MOVE_CALLorMOVE (custom)
    // - CLOSURE_SETTABLE (custom)
    // - TEST
    // - LOADNIL
    // - GETUPVAL
    // - SELF_CALLorGETTABUP (custom)
    // - MOVE
    // - LT
    // - LE
    // - GETTABLE_GETTABLE_GETTABLE (custom)
    // - EQ
    private static void ReadCode(FGFPLuaArchive Ar, LuaFunction func)
    {
        var sizeCode = Ar.Read<int>();
        func.Code = Ar.ReadBytes(sizeCode * 4);
        // MapOpcodes(func.Code, opcodeMapping);
    }

    private static void ReadConstants(FGFPLuaArchive Ar, LuaFunction func)
    {
        var sizeK = Ar.Read<int>();
        func.Constants = new LuaConstant[sizeK];
        for (int i = 0; i < sizeK; i++)
            func.Constants[i] = ReadConstant(Ar);
    }

    private static LuaConstant ReadConstant(FGFPLuaArchive Ar)
    {
        var constant = new LuaConstant
        {
            Type = Ar.Read<byte>()
        };

        switch (constant.Type)
        {
            case 0:  // LUA_TNIL
                break;
            case 1:  // LUA_TBOOLEAN
                constant.Data = [Ar.Read<byte>()];
                break;
            case 3:  // LUA_TNUMFLT
                constant.Data = Ar.ReadBytes(8);
                break;
            case 19: // LUA_TNUMINT
                constant.Data = Ar.ReadBytes(8);
                break;
            case 4:  // LUA_TSHRSTR
            case 20: // LUA_TLNGSTR
                constant.StrData = Ar.ReadLuaString();
                break;
        }

        return constant;
    }

    private static void ReadUpvalues(FGFPLuaArchive Ar, LuaFunction func)
    {
        var sizeUpValues = Ar.Read<byte>(); // int -> byte
        func.Upvalues = new LuaUpvalue[sizeUpValues];
        for (int i = 0; i < sizeUpValues; i++)
        {
            func.Upvalues[i] = new LuaUpvalue
            {
                Instack = Ar.Read<byte>(),
                Idx = Ar.Read<byte>()
            };
        }
    }

    private static void ReadProtos(FGFPLuaArchive Ar, LuaFunction func)
    {
        var sizeP = Ar.Read<short>(); // int -> short
        func.Protos = new LuaFunction[sizeP];
        for (int i = 0; i < sizeP; i++)
            func.Protos[i] = ReadFunction(Ar, func.SourceName);
    }

    private static void ReadDebug(FGFPLuaArchive Ar, LuaFunction func)
    {
        var debug = new LuaDebug();

        var sizeLineInfo = Ar.Read<int>();
        debug.SizeLineInfo = (ulong) sizeLineInfo;

        var expandedLineInfo = new byte[sizeLineInfo * 4]; // 4 -> 2, I expand so I don't have to change writer
        for (int i = 0; i < sizeLineInfo; i++)
        {
            var shortBytes = Ar.ReadBytes(2);
            expandedLineInfo[i * 4] = shortBytes[0];
            expandedLineInfo[i * 4 + 1] = shortBytes[1];
        }

        debug.LineInfo = expandedLineInfo;

        var sizeLocVars = Ar.Read<short>(); // int -> short
        debug.LocVars = new LuaLocVar[sizeLocVars];
        for (int i = 0; i < sizeLocVars; i++)
        {
            debug.LocVars[i] = new LuaLocVar
            {
                NameData = Ar.ReadLuaString(),
                StartPc = (ulong) Ar.Read<int>(),
                EndPc = (ulong) Ar.Read<int>()
            };
        }

        var sizeUpvalueNames = Ar.Read<byte>(); // int -> byte
        debug.UpvalueNames = new LuaUpvalueName[sizeUpvalueNames];
        for (int i = 0; i < sizeUpvalueNames; i++)
        {
            debug.UpvalueNames[i] = new LuaUpvalueName
            {
                NameData = Ar.ReadLuaString()
            };
        }

        func.Debug = debug;
    }

    //private static void MapOpcodes(byte[] code, Dictionary<byte, byte>? opcodeMapping)
    //{
    //    if (opcodeMapping is null)
    //        return;

    //    for (int i = 0; i < code.Length; i += 4)
    //    {
    //        uint instr = BitConverter.ToUInt32(code, i);
    //        byte opcode = (byte) (instr & 0x3F);

    //        if (!opcodeMapping.TryGetValue(opcode, out var mapped))
    //            continue;

    //        instr = (instr & ~0x3Fu) | (uint) (mapped & 0x3F);

    //        var bytes = BitConverter.GetBytes(instr);
    //        Array.Copy(bytes, 0, code, i, 4);
    //    }
    //}
}

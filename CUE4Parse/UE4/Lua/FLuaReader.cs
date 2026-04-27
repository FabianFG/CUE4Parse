using System;
using System.Collections.Generic;
using System.Linq;

namespace CUE4Parse.UE4.Lua;

public class LuaBytecode
{
    public LuaHeader Header { get; set; } = new();
    public LuaFunction MainFunc { get; set; } = new();
}

public struct LuaHeader
{
    public byte[] Signature { get; set; }
    public byte Version { get; set; }
    public byte Format { get; set; }
    public byte[] LuacData { get; set; }
    public byte InstructionSize { get; set; }
    public byte IntegerSize { get; set; }
    public byte NumberSize { get; set; }
    public byte[] LuacInt { get; set; }
    public byte[] LuacNum { get; set; }
    public byte Closure { get; set; }
}

public class LuaFunction
{
    public string SourceName { get; set; } = string.Empty;
    public ulong LineDefined { get; set; }
    public ulong LastLineDefined { get; set; }
    public byte NumParams { get; set; }
    public byte IsVarArg { get; set; }
    public byte MaxStackSize { get; set; }

    public byte[] Code { get; set; } = [];
    public LuaConstant[] Constants { get; set; } = [];
    public LuaUpvalue[] Upvalues { get; set; } = [];
    public LuaFunction[] Protos { get; set; } = [];
    public LuaDebug Debug { get; set; } = new();
}

public class LuaConstant
{
    public byte Type { get; set; }
    public byte[] Data { get; set; } = [];
    public string StrData { get; set; } = string.Empty;
}

public class LuaUpvalue
{
    public byte Instack { get; set; }
    public byte Idx { get; set; }
    public byte Kind { get; set; }
}

public class LuaDebug
{
    public ulong SizeLineInfo { get; set; }
    public byte[] LineInfo { get; set; } = [];
    public LuaAbsLineInfo[] AbsLineInfo { get; set; } = [];
    public LuaLocVar[] LocVars { get; set; } = [];
    public LuaUpvalueName[] UpvalueNames { get; set; } = [];
}

public class LuaAbsLineInfo
{
    public ulong Pc { get; set; }
    public ulong Line { get; set; }
}

public class LuaLocVar
{
    public string NameData { get; set; } = string.Empty;
    public ulong StartPc { get; set; }
    public ulong EndPc { get; set; }
}

public class LuaUpvalueName
{
    public string NameData { get; set; } = string.Empty;
}

public static class FLuaReader
{
    // Standard Lua 5.4 bytecode reader
    public static LuaBytecode ReadLua54(FLuaArchive Ar, Dictionary<byte, byte>? opcodeMapping = null) => new()
    {
        Header = ReadHeader(Ar),
        MainFunc = ReadFunction(Ar, opcodeMapping)
    };

    public static LuaHeader ReadHeader(FLuaArchive Ar) => new()
    {
        Signature = Ar.ReadBytes(4),
        Version = Ar.Read<byte>(),
        Format = Ar.Read<byte>(),
        LuacData = Ar.ReadBytes(6),
        InstructionSize = Ar.Read<byte>(),
        IntegerSize = Ar.Read<byte>(),
        NumberSize = Ar.Read<byte>(),
        LuacInt = Ar.ReadBytes(8),
        LuacNum = Ar.ReadBytes(8),
        Closure = Ar.Read<byte>()
    };

    public static LuaFunction ReadFunction(FLuaArchive Ar, Dictionary<byte, byte>? opcodeMapping = null)
    {
        var func = new LuaFunction
        {
            SourceName = Ar.ReadLuaString(),
            LineDefined = Ar.ReadLuaInt(),
            LastLineDefined = Ar.ReadLuaInt(),
            NumParams = Ar.Read<byte>(),
            IsVarArg = Ar.Read<byte>(),
            MaxStackSize = Ar.Read<byte>(),
            Code = [.. Ar.ReadLuaArray(() => Ar.ReadBytes(4)).SelectMany(x => x)]
        };

        MapOpcodes(func.Code, opcodeMapping);

        func.Constants = Ar.ReadLuaArray(() => ReadConstant(Ar));
        func.Upvalues = Ar.ReadLuaArray(() => ReadUpvalue(Ar));
        func.Protos = Ar.ReadLuaArray(() => ReadFunction(Ar, opcodeMapping));
        func.Debug = ReadDebug(Ar);

        return func;
    }

    private static void MapOpcodes(byte[] code, Dictionary<byte, byte>? opcodeMapping)
    {
        if (opcodeMapping is null)
            return;

        for (int i = 0; i < code.Length; i += 4)
        {
            uint instr = BitConverter.ToUInt32(code, i);
            byte opcode = (byte) (instr & 0x7F);

            if (opcodeMapping.TryGetValue(opcode, out byte mapped))
            {
                instr = (instr & ~0x7Fu) | (uint) (mapped & 0x7F);
                byte[] bytes = BitConverter.GetBytes(instr);
                Array.Copy(bytes, 0, code, i, 4);
            }
        }
    }

    public static LuaConstant ReadConstant(FLuaArchive Ar)
    {
        var constant = new LuaConstant
        {
            Type = Ar.Read<byte>()
        };

        switch (constant.Type & 0x3F)
        {
            case 3:  // Float
            case 19: // Integer
                constant.Data = Ar.ReadBytes(8);
                break;
            case 4:  // Short String
            case 20: // Long String
                constant.StrData = Ar.ReadLuaString();
                break;
        }

        return constant;
    }

    public static LuaUpvalue ReadUpvalue(FLuaArchive Ar) => new()
    {
        Instack = Ar.Read<byte>(),
        Idx = Ar.Read<byte>(),
        Kind = Ar.Read<byte>()
    };

    public static LuaDebug ReadDebug(FLuaArchive Ar)
    {
        var debug = new LuaDebug
        {
            SizeLineInfo = Ar.ReadLuaInt()
        };

        debug.LineInfo = Ar.ReadBytes((int) debug.SizeLineInfo);
        debug.AbsLineInfo = Ar.ReadLuaArray(() => new LuaAbsLineInfo
        {
            Pc = Ar.ReadLuaInt(),
            Line = Ar.ReadLuaInt()
        });
        debug.LocVars = Ar.ReadLuaArray(() => new LuaLocVar
        {
            NameData = Ar.ReadLuaString(),
            StartPc = Ar.ReadLuaInt(),
            EndPc = Ar.ReadLuaInt()
        });
        debug.UpvalueNames = Ar.ReadLuaArray(() => new LuaUpvalueName
        {
            NameData = Ar.ReadLuaString()
        });

        return debug;
    }

    public static bool IsValidLuaMagic(byte[] data) => data.AsSpan() is [0x1B, 0x4C, 0x75, 0x61, ..]; // Lua magic "\x1BLua"
}

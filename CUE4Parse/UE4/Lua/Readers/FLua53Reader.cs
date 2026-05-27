using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Lua.Archives;

namespace CUE4Parse.UE4.Lua.Readers;

// Standard Lua 5.3 bytecode reader
public class FLua53Reader
{
    public static LuaBytecode ReadLuaBytecode(FLua53Archive Ar, Dictionary<byte, byte>? opcodeMapping = null) => new()
    {
        Header = ReadHeader(Ar),
        SizeUpvalues = Ar.Read<byte>(),
        MainFunc = ReadFunction(Ar, null, opcodeMapping)
    };

    public static LuaHeader ReadHeader(FLua53Archive Ar) => new()
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

    public static LuaFunction ReadFunction(FLua53Archive Ar, string? parentSource, Dictionary<byte, byte>? opcodeMapping = null)
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

        ReadCode(Ar, func, opcodeMapping);
        ReadConstants(Ar, func);
        ReadUpvalues(Ar, func);
        ReadProtos(Ar, func, opcodeMapping);
        ReadDebug(Ar, func);

        return func;
    }

    public static void ReadCode(FLua53Archive Ar, LuaFunction func, Dictionary<byte, byte>? opcodeMapping = null)
    {
        var sizeCode = Ar.Read<int>();
        func.Code = Ar.ReadBytes(sizeCode * 4);
        MapOpcodes(func.Code, opcodeMapping);
    }

    private static void MapOpcodes(byte[] code, Dictionary<byte, byte>? opcodeMapping)
    {
        if (opcodeMapping is null)
            return;

        for (int i = 0; i < code.Length; i += 4)
        {
            uint instr = BitConverter.ToUInt32(code, i);
            byte opcode = (byte) (instr & 0x3F);

            if (opcodeMapping.TryGetValue(opcode, out byte mapped))
            {
                instr = (instr & ~0x3Fu) | (uint) (mapped & 0x3F);
                byte[] bytes = BitConverter.GetBytes(instr);
                Array.Copy(bytes, 0, code, i, 4);
            }
        }
    }

    public static void ReadConstants(FLua53Archive Ar, LuaFunction func)
    {
        var sizeK = Ar.Read<int>();
        func.Constants = new LuaConstant[sizeK];
        for (int i = 0; i < sizeK; i++)
            func.Constants[i] = ReadConstant(Ar);
    }

    public static LuaConstant ReadConstant(FLua53Archive Ar)
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

    public static void ReadUpvalues(FLua53Archive Ar, LuaFunction func)
    {
        var sizeUpValues = Ar.Read<int>();
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

    private static void ReadProtos(FLua53Archive Ar, LuaFunction func, Dictionary<byte, byte>? opcodeMapping = null)
    {
        var sizeP = Ar.Read<int>();
        func.Protos = new LuaFunction[sizeP];
        for (int i = 0; i < sizeP; i++)
            func.Protos[i] = ReadFunction(Ar, func.SourceName, opcodeMapping);
    }

    private static void ReadDebug(FLua53Archive Ar, LuaFunction func)
    {
        var debug = new LuaDebug();

        var sizeLineInfo = Ar.Read<int>();
        debug.SizeLineInfo = (ulong) sizeLineInfo;
        debug.LineInfo = Ar.ReadBytes(sizeLineInfo * 4);

        var sizeLocVars = Ar.Read<int>();
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

        var sizeUpvalueNames = Ar.Read<int>();
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
}

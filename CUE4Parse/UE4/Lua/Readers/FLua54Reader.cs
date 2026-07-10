using CUE4Parse.UE4.Lua.Archives;

namespace CUE4Parse.UE4.Lua.Readers;

// Standard Lua 5.4 bytecode reader
public static class FLua54Reader
{
    public static LuaBytecode ReadLuaBytecode(FLua54Archive Ar, Dictionary<byte, byte>? opcodeMapping = null) => new()
    {
        Header = ReadHeader(Ar),
        MainFunc = ReadFunction(Ar, opcodeMapping)
    };

    public static LuaHeader ReadHeader(FLua54Archive Ar) => new()
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

    public static LuaFunction ReadFunction(FLua54Archive Ar, Dictionary<byte, byte>? opcodeMapping = null)
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

    public static LuaConstant ReadConstant(FLua54Archive Ar)
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

    public static LuaUpvalue ReadUpvalue(FLua54Archive Ar) => new()
    {
        Instack = Ar.Read<byte>(),
        Idx = Ar.Read<byte>(),
        Kind = Ar.Read<byte>()
    };

    public static LuaDebug ReadDebug(FLua54Archive Ar)
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
}

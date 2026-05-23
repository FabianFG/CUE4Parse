using System;

namespace CUE4Parse.UE4.Lua.Readers;

public class LuaBytecode
{
    public LuaHeader Header { get; set; } = new();
    public byte SizeUpvalues { get; set; } // Lua 5.3
    public LuaFunction MainFunc { get; set; } = new();
}

public struct LuaHeader
{
    public byte[] Signature { get; set; }
    public byte Version { get; set; }
    public byte Format { get; set; }
    public byte[] LuacData { get; set; }

    public byte CintSize { get; set; } // Lua 5.3
    public byte SizeTSize { get; set; } // Lua 5.3

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
    // Static data in the header
    public static readonly byte[] LUAC_DATA = [0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] LUAC_INT = [0x78, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    public static readonly byte[] LUAC_NUM = BitConverter.GetBytes(370.5);
    public static readonly byte LUAC_FORMAT = 0; // This is the official format

    public static bool IsValidLuaMagic(byte[] data) => data.AsSpan() is [0x1B, 0x4C, 0x75, 0x61, ..]; // Lua magic "\x1BLua"
}

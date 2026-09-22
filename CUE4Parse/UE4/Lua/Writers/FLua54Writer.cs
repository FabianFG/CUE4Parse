using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;

namespace CUE4Parse.UE4.Lua.Writers;

// Standard Lua 5.4 bytecode writer
public sealed class FLuaWriter54
{
    private readonly FLua54ArchiveWriter Ar = new();

    public FLuaWriter54(LuaBytecode bytecode)
    {
        WriteHeader(bytecode.Header);
        WriteFunction(bytecode.MainFunc);
    }

    public byte[] GetBuffer() => Ar.GetBuffer();

    private void WriteHeader(LuaHeader header)
    {
        Ar.Write(header.Signature);
        Ar.Write(header.Version);
        Ar.Write(header.Format);
        Ar.Write(header.LuacData);
        Ar.Write(header.InstructionSize);
        Ar.Write(header.IntegerSize);
        Ar.Write(header.NumberSize);
        Ar.Write(header.LuacInt);
        Ar.Write(header.LuacNum);
        Ar.Write(header.Closure);
    }

    private void WriteFunction(LuaFunction function)
    {
        Ar.WriteLuaString(function.SourceName);
        Ar.WriteLuaInt(function.LineDefined);
        Ar.WriteLuaInt(function.LastLineDefined);

        Ar.Write(function.NumParams);
        Ar.Write(function.IsVarArg);
        Ar.Write(function.MaxStackSize);

        WriteCode(function);
        WriteConstants(function);
        WriteUpvalues(function);

        Ar.WriteLuaArray(function.Protos, WriteFunction);

        WriteDebug(function.Debug);
    }

    private void WriteCode(LuaFunction function)
    {
        Ar.WriteLuaInt((ulong) (function.Code.Length / sizeof(uint)));
        Ar.Write(function.Code);
    }

    private void WriteConstants(LuaFunction function)
    {
        Ar.WriteLuaArray(function.Constants, constant =>
        {
            Ar.Write(constant.Type);
            switch (constant.Type & 0x3F)
            {
                case 3:  // LUA_VNUMFLT; Float
                case 19: // LUA_VNUMINT; Integer
                    Ar.Write(constant.Data);
                    break;
                case 4:  // LUA_VSHRSTR; Short string
                case 20: // LUA_VLNGSTR; Long string
                    Ar.WriteLuaString(constant.StrData);
                    break;
            }
        });
    }

    private void WriteUpvalues(LuaFunction function)
    {
        Ar.WriteLuaArray(function.Upvalues, upvalue =>
        {
            Ar.Write(upvalue.Instack);
            Ar.Write(upvalue.Idx);
            Ar.Write(upvalue.Kind);
        });
    }

    private void WriteDebug(LuaDebug debug)
    {
        Ar.WriteLuaInt(debug.SizeLineInfo);
        Ar.Write(debug.LineInfo);

        Ar.WriteLuaArray(debug.AbsLineInfo, info =>
        {
            Ar.WriteLuaInt(info.Pc);
            Ar.WriteLuaInt(info.Line);
        });

        Ar.WriteLuaArray(debug.LocVars, local =>
        {
            Ar.WriteLuaString(local.NameData);
            Ar.WriteLuaInt(local.StartPc);
            Ar.WriteLuaInt(local.EndPc);
        });

        Ar.WriteLuaArray(debug.UpvalueNames, name =>
        {
            Ar.WriteLuaString(name.NameData);
        });
    }
}

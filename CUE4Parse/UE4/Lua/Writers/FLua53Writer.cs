using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;

namespace CUE4Parse.UE4.Lua.Writers;

// Standard Lua 5.3 bytecode writer
public sealed class FLuaWriter53
{
    private readonly FLua53ArchiveWriter Ar = new();

    public FLuaWriter53(LuaBytecode bytecode)
    {
        WriteHeader(bytecode.Header);
        Ar.Write(bytecode.SizeUpvalues);
        WriteFunction(bytecode.MainFunc);
    }

    public byte[] GetBuffer() => Ar.GetBuffer();

    private void WriteHeader(LuaHeader header)
    {
        Ar.Write(header.Signature);
        Ar.Write(header.Version);
        Ar.Write(header.Format);
        Ar.Write(header.LuacData);

        Ar.Write(header.CintSize);
        Ar.Write(header.SizeTSize);

        Ar.Write(header.InstructionSize);
        Ar.Write(header.IntegerSize);
        Ar.Write(header.NumberSize);

        Ar.Write(header.LuacInt);
        Ar.Write(header.LuacNum);
    }

    private void WriteFunction(LuaFunction function)
    {
        Ar.WriteLuaString(function.SourceName);

        Ar.Write((uint) function.LineDefined);
        Ar.Write((uint) function.LastLineDefined);

        Ar.Write(function.NumParams);
        Ar.Write(function.IsVarArg);
        Ar.Write(function.MaxStackSize);

        WriteCode(function);
        WriteConstants(function);
        WriteUpvalues(function);
        WriteProtos(function);
        WriteDebug(function.Debug);
    }

    private void WriteCode(LuaFunction function)
    {
        Ar.Write(function.Code.Length / sizeof(uint));
        Ar.Write(function.Code);
    }

    private void WriteConstants(LuaFunction function)
    {
        Ar.Write(function.Constants.Length);
        foreach (var constant in function.Constants)
        {
            Ar.Write(constant.Type);

            switch (constant.Type)
            {
                case 0: // LUA_TNIL; Null
                    break;
                case 1: // LUA_TBOOLEAN; Boolean
                    Ar.Write(constant.Data[0]);
                    break;
                case 3:  // LUA_TNUMFLT; Float
                case 19: // LUA_TNUMINT; Integer
                    Ar.Write(constant.Data);
                    break;
                case 4:  // LUA_TSHRSTR; Short string
                case 20: // LUA_TLNGSTR; Long string
                    Ar.WriteLuaString(constant.StrData);
                    break;
            }
        }
    }

    private void WriteUpvalues(LuaFunction function)
    {
        Ar.Write(function.Upvalues.Length);

        foreach (var upvalue in function.Upvalues)
        {
            Ar.Write(upvalue.Instack);
            Ar.Write(upvalue.Idx);
        }
    }

    private void WriteProtos(LuaFunction function)
    {
        Ar.Write(function.Protos.Length);

        foreach (var proto in function.Protos)
            WriteFunction(proto);
    }

    private void WriteDebug(LuaDebug debug)
    {
        Ar.Write((int) debug.SizeLineInfo);
        Ar.Write(debug.LineInfo);

        Ar.Write(debug.LocVars.Length);
        foreach (var local in debug.LocVars)
        {
            Ar.WriteLuaString(local.NameData);
            Ar.Write((int) local.StartPc);
            Ar.Write((int) local.EndPc);
        }

        Ar.Write(debug.UpvalueNames.Length);
        foreach (var name in debug.UpvalueNames)
            Ar.WriteLuaString(name.NameData);
    }
}

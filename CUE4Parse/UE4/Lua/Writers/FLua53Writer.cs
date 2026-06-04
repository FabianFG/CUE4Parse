using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;

namespace CUE4Parse.UE4.Lua.Writers;

// Standard Lua 5.3 bytecode writer
public static class FLuaWriter53
{
    public static void Write(FLua53ArchiveWriter writer, LuaBytecode l)
    {
        WriteHeader(writer, l.Header);
        writer.Write(l.SizeUpvalues);
        WriteFunction(writer, l.MainFunc);
    }

    private static void WriteHeader(FLua53ArchiveWriter writer, LuaHeader h)
    {
        writer.Write(h.Signature);
        writer.Write(h.Version);
        writer.Write(h.Format);
        writer.Write(h.LuacData);

        writer.Write(h.CintSize);
        writer.Write(h.SizeTSize);

        writer.Write(h.InstructionSize);
        writer.Write(h.IntegerSize);
        writer.Write(h.NumberSize);

        writer.Write(h.LuacInt);
        writer.Write(h.LuacNum);
    }

    private static void WriteFunction(FLua53ArchiveWriter writer, LuaFunction f)
    {
        writer.WriteLuaString(f.SourceName);

        writer.Write((uint) f.LineDefined);
        writer.Write((uint) f.LastLineDefined);

        writer.Write(f.NumParams);
        writer.Write(f.IsVarArg);
        writer.Write(f.MaxStackSize);

        WriteCode(writer, f);
        WriteConstants(writer, f);
        WriteUpvalues(writer, f);
        WriteProtos(writer, f);
        WriteDebug(writer, f.Debug);
    }

    private static void WriteCode(FLua53ArchiveWriter writer, LuaFunction f)
    {
        int sizeCode = f.Code.Length / 4;

        writer.Write(sizeCode);
        writer.Write(f.Code);
    }

    private static void WriteConstants(FLua53ArchiveWriter writer, LuaFunction f)
    {
        writer.Write(f.Constants.Length);

        foreach (var c in f.Constants)
        {
            writer.Write(c.Type);
            switch (c.Type)
            {
                case 0: // LUA_TNIL
                    break;
                case 1: // LUA_TBOOLEAN
                    writer.Write(c.Data[0]);
                    break;
                case 3:  // LUA_TNUMFLT
                case 19: // LUA_TNUMINT
                    writer.Write(c.Data);
                    break;
                case 4:  // LUA_TSHRSTR
                case 20: // LUA_TLNGSTR
                    writer.WriteLuaString(c.StrData);
                    break;
            }
        }
    }

    private static void WriteUpvalues(FLua53ArchiveWriter writer, LuaFunction f)
    {
        writer.Write(f.Upvalues.Length);

        foreach (var u in f.Upvalues)
        {
            writer.Write(u.Instack);
            writer.Write(u.Idx);
        }
    }

    private static void WriteProtos(FLua53ArchiveWriter writer, LuaFunction f)
    {
        writer.Write(f.Protos.Length);
        foreach (var p in f.Protos)
            WriteFunction(writer, p);
    }

    private static void WriteDebug(FLua53ArchiveWriter writer, LuaDebug d)
    {
        writer.Write((int) d.SizeLineInfo);
        writer.Write(d.LineInfo);

        writer.Write(d.LocVars.Length);
        foreach (var v in d.LocVars)
        {
            writer.WriteLuaString(v.NameData);

            writer.Write((int) v.StartPc);
            writer.Write((int) v.EndPc);
        }

        writer.Write(d.UpvalueNames.Length);
        foreach (var un in d.UpvalueNames)
            writer.WriteLuaString(un.NameData);
    }
}

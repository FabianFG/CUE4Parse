namespace CUE4Parse.UE4.Lua;

// Standard Lua 5.4 bytecode writer
public static class FLuaWriter54
{
    public static void Write(FLuaArchiveWriter writer, LuaBytecode l)
    {
        WriteHeader(writer, l.Header);
        WriteFunction(writer, l.MainFunc);
    }

    private static void WriteHeader(FLuaArchiveWriter writer, LuaHeader h)
    {
        writer.Write(h.Signature);
        writer.Write(h.Version);
        writer.Write(h.Format);
        writer.Write(h.LuacData);
        writer.Write(h.InstructionSize);
        writer.Write(h.IntegerSize);
        writer.Write(h.NumberSize);
        writer.Write(h.LuacInt);
        writer.Write(h.LuacNum);
        writer.Write(h.Closure);
    }

    private static void WriteFunction(FLuaArchiveWriter writer, LuaFunction f)
    {
        writer.WriteLuaString(f.SourceName);
        writer.WriteLuaInt(f.LineDefined);
        writer.WriteLuaInt(f.LastLineDefined);

        writer.Write(f.NumParams);
        writer.Write(f.IsVarArg);
        writer.Write(f.MaxStackSize);

        writer.WriteLuaInt((ulong) (f.Code.Length / 4));
        writer.Write(f.Code);

        writer.WriteLuaArray(f.Constants, c =>
        {
            writer.Write(c.Type);
            int type = c.Type & 0x3F;
            switch (type)
            {
                case 3:  // Float
                case 19: // Integer
                    writer.Write(c.Data);
                    break;
                case 4:  // Short String
                case 20: // Long String
                    writer.WriteLuaString(c.StrData);
                    break;
            }
        });

        writer.WriteLuaArray(f.Upvalues, u =>
        {
            writer.Write(u.Instack);
            writer.Write(u.Idx);
            writer.Write(u.Kind);
        });

        writer.WriteLuaArray(f.Protos, p => WriteFunction(writer, p));

        WriteDebug(writer, f.Debug);
    }

    private static void WriteDebug(FLuaArchiveWriter writer, LuaDebug d)
    {
        writer.WriteLuaInt(d.SizeLineInfo);
        writer.Write(d.LineInfo);

        writer.WriteLuaArray(d.AbsLineInfo, abs =>
        {
            writer.WriteLuaInt(abs.Pc);
            writer.WriteLuaInt(abs.Line);
        });

        writer.WriteLuaArray(d.LocVars, v =>
        {
            writer.WriteLuaString(v.NameData);
            writer.WriteLuaInt(v.StartPc);
            writer.WriteLuaInt(v.EndPc);
        });

        writer.WriteLuaArray(d.UpvalueNames, un =>
        {
            writer.WriteLuaString(un.NameData);
        });
    }
}


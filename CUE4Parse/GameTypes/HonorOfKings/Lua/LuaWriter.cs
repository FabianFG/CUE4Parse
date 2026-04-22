using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CUE4Parse.GameTypes.HonorOfKings.Lua;

public class FNGRLuaWriter(Stream stream) : BinaryWriter(stream)
{
    public void WriteLuaInt(ulong v)
    {
        if (v == 0)
        {
            Write((byte) 0x80);
            return;
        }

        var bytes = new List<byte>();
        bool first = true;
        while (v > 0 || first)
        {
            byte x = (byte) (v & 0x7F);
            if (first)
                x |= 0x80;

            bytes.Add(x);
            v >>= 7;
            first = false;
        }
        bytes.Reverse();
        Write(bytes.ToArray());
    }

    public void WriteLuaString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteLuaInt(0);
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(value);

        WriteLuaInt((ulong) buffer.Length + 1);
        Write(buffer);
    }

    public void WriteLuaArray<T>(T[] array, Action<T> writeElement)
    {
        if (array == null)
        {
            WriteLuaInt(0);
            return;
        }

        WriteLuaInt((ulong) array.Length);
        foreach (var item in array)
        {
            writeElement(item);
        }
    }
}

public static class LuaWriter
{
    public static void Write(FNGRLuaWriter writer, LuaBytecode l)
    {
        WriteHeader(writer, l.Header);
        WriteFunction(writer, l.MainFunc);
    }

    private static void WriteHeader(FNGRLuaWriter writer, LuaHeader h)
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

    private static void WriteFunction(FNGRLuaWriter writer, LuaFunction f)
    {
        writer.WriteLuaString(f.SourceName);
        writer.WriteLuaInt(f.LineDefined);
        writer.WriteLuaInt(f.LastLineDefined);

        writer.Write(f.NumParams);
        writer.Write(f.IsVarArg);
        writer.Write(f.MaxStackSize);

        writer.WriteLuaInt((ulong)(f.Code.Length / 4));
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

    private static void WriteDebug(FNGRLuaWriter writer, LuaDebug d)
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

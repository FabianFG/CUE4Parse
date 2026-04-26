using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Lua;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.HonorOfKings.Lua;

public class FNGRLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLuaArchive(name, data, versions)
{
    public T ReadBE<T>() where T : unmanaged
    {
        T value = Read<T>();

        return value switch
        {
            ushort v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            uint v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            ulong v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            short v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            int v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            long v => (T) (object) BinaryPrimitives.ReverseEndianness(v),
            _ => value
        };
    }
}

public class LuaBytecode(FNGRLuaArchive Ar)
{
    public LuaHeader Header = new(Ar);
    public LuaFunction MainFunc = new(Ar);
}

public struct LuaHeader(FNGRLuaArchive Ar)
{
    public byte[] Signature = Ar.ReadBytes(4);
    public byte Version = Ar.Read<byte>();
    public byte Format = Ar.Read<byte>();
    public byte[] LuacData = Ar.ReadBytes(6);
    public byte InstructionSize = Ar.Read<byte>();
    public byte IntegerSize = Ar.Read<byte>();
    public byte NumberSize = Ar.Read<byte>();
    public byte[] LuacInt = Ar.ReadBytes(8);
    public byte[] LuacNum = Ar.ReadBytes(8);
    public byte Closure = Ar.Read<byte>();
}

public class LuaFunction
{
    public string SourceName;
    public ulong LineDefined;
    public ulong LastLineDefined;
    public byte NumParams;
    public byte IsVarArg;
    public byte MaxStackSize;
    public byte[] Code;
    public LuaConstant[] Constants;
    public LuaUpvalue[] Upvalues;
    public LuaFunction[] Protos;
    public LuaDebug Debug;

    private static readonly Dictionary<byte, byte> _opcodeMapping = new()
    {
        { 0x0A, 0x00 },
        { 0x00, 0x0A },
        { 0x07, 0x03 },
        { 0x03, 0x07 },
        { 0x01, 0x09 },
        { 0x09, 0x01 },
        { 0x02, 0x08 },
        { 0x08, 0x02 },
        { 0x04, 0x06 },
        { 0x06, 0x04 },
    };

    public LuaFunction(FNGRLuaArchive Ar)
    {
        SourceName = Ar.ReadLuaString();
        LineDefined = Ar.ReadLuaInt();
        LastLineDefined = Ar.ReadLuaInt();

        NumParams = Ar.Read<byte>();
        IsVarArg = Ar.Read<byte>();
        MaxStackSize = Ar.Read<byte>();

        Code = [.. Ar.ReadLuaArray(() => Ar.ReadBytes(4)).SelectMany(x => x)];
        MapOpcodes(Code); // Opcode is shuffled

        Constants = Ar.ReadLuaArray(() => new LuaConstant(Ar));
        Upvalues = Ar.ReadLuaArray(() => new LuaUpvalue(Ar));
        Protos = Ar.ReadLuaArray(() => new LuaFunction(Ar));
        Debug = new LuaDebug(Ar);
    }

    private static void MapOpcodes(byte[] code)
    {
        for (int i = 0; i < code.Length; i += 4)
        {
            uint instr = BitConverter.ToUInt32(code, i);
            byte opcode = (byte)(instr & 0x7F);

            if (_opcodeMapping.TryGetValue(opcode, out byte mapped))
            {
                instr = (instr & ~0x7Fu) | (uint)(mapped & 0x7F);
                byte[] bytes = BitConverter.GetBytes(instr);
                Array.Copy(bytes, 0, code, i, 4);
            }
        }
    }
}

public class LuaConstant
{
    public byte Type;
    public byte[] Data = [];
    public string StrData = string.Empty;

    public LuaConstant(FNGRLuaArchive Ar)
    {
        Type = Ar.Read<byte>();
        int typeValue = Type & 0x3F;

        switch (typeValue)
        {
            case 3:  // Float
            case 19: // Integer
                Data = Ar.ReadBytes(8);
                break;
            case 4:  // Short String
            case 20: // Long String
                StrData = Ar.ReadLuaString();
                break;
        }
    }
}

public class LuaUpvalue(FNGRLuaArchive Ar)
{
    public byte Instack = Ar.Read<byte>();
    public byte Idx = Ar.Read<byte>();
    public byte Kind = Ar.Read<byte>();
}

public class LuaDebug
{
    public ulong SizeLineInfo;
    public byte[] LineInfo;
    public LuaAbsLineInfo[] AbsLineInfo;
    public LuaLocVar[] LocVars;
    public LuaUpvalueName[] UpvalueNames;

    public LuaDebug(FNGRLuaArchive Ar)
    {
        SizeLineInfo = Ar.ReadLuaInt();
        LineInfo = Ar.ReadBytes((int)SizeLineInfo);
        AbsLineInfo = Ar.ReadLuaArray(() => new LuaAbsLineInfo(Ar));
        LocVars = Ar.ReadLuaArray(() => new LuaLocVar(Ar));
        UpvalueNames = Ar.ReadLuaArray(() => new LuaUpvalueName(Ar));
    }
}

public class LuaAbsLineInfo(FNGRLuaArchive Ar)
{
    public ulong Pc = Ar.ReadLuaInt();
    public ulong Line = Ar.ReadLuaInt();
}

public class LuaLocVar(FNGRLuaArchive Ar)
{
    public string NameData = Ar.ReadLuaString();
    public ulong StartPc = Ar.ReadLuaInt();
    public ulong EndPc = Ar.ReadLuaInt();
}

public class LuaUpvalueName(FNGRLuaArchive Ar)
{
    public string NameData = Ar.ReadLuaString();
}

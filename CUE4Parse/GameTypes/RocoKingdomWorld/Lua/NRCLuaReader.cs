using System;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Lua;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Lua;

public class FNRCLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLuaArchive(name, data, versions)
{
    // Strings are encrypted
    public string ReadNRCString()
    {
        ulong size = ReadLuaInt();
        if (size <= 1)
            return string.Empty;

        int length = (int) size - 1;
        byte[] b = ReadBytes(length);

        int seed = length + 3;
        for (int i = 0; i < length; i++)
        {
            b[i] = (byte) (b[i] ^ (seed + i));
        }

        return Encoding.UTF8.GetString(b);
    }
}

public class LuaBytecode(FNRCLuaArchive reader)
{
    public LuaHeader Header = new(reader);
    public LuaFunction MainFunc = new(reader);
}

public struct LuaHeader(FNRCLuaArchive reader)
{
    public byte[] Signature = reader.ReadBytes(4);
    public byte Version = reader.Read<byte>();
    public byte Format = reader.Read<byte>();
    public byte[] LuacData = reader.ReadBytes(6);
    public byte InstructionSize = reader.Read<byte>();
    public byte IntegerSize = reader.Read<byte>();
    public byte NumberSize = reader.Read<byte>();
    public byte[] LuacInt = reader.ReadBytes(8);
    public byte[] LuacNum = reader.ReadBytes(8);
    public byte Closure = reader.Read<byte>();
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

    private static readonly byte[] _opcodeMapping = [
        0, 66, 70, 55, 20, 17, 73, 28, 46, 68, 52, 42, 43, 19, 77, 21,
        35, 81, 8, 13, 7, 18, 79, 6, 59, 60, 67, 64, 10, 24, 39, 34,
        31, 53, 75, 58, 37, 3, 26, 72, 5, 51, 23, 9, 74, 76, 36, 71,
        63, 56, 65, 25, 57, 62, 22, 32, 12, 40, 33, 38, 2, 47, 44, 69,
        1, 49, 48, 50, 30, 45, 27, 78, 16, 80, 11, 54, 4, 41, 15, 14,
        61, 29, 82
    ];

    public LuaFunction(FNRCLuaArchive Ar)
    {
        SourceName = Ar.ReadNRCString();
        LineDefined = Ar.ReadLuaInt();
        Code = [.. Ar.ReadLuaArray(() => Ar.ReadBytes(4)).SelectMany(x => x)];
        MapOpcodes(Code); // Opcode is shuffled
        LastLineDefined = Ar.ReadLuaInt(); // Shuffled
        NumParams = Ar.Read<byte>(); // Shuffled
        Constants = Ar.ReadLuaArray(() => new LuaConstant(Ar));
        IsVarArg = Ar.Read<byte>(); // Shuffled
        Upvalues = ReadUpvaluesInternal(Ar);
        Protos = Ar.ReadLuaArray(() => new LuaFunction(Ar));
        Debug = new LuaDebug(Ar);
    }

    private LuaUpvalue[] ReadUpvaluesInternal(FNRCLuaArchive Ar)
    {
        int sizeUp = (int)Ar.ReadLuaInt();
        MaxStackSize = Ar.Read<byte>(); // Shuffled
        var upvalues = new LuaUpvalue[sizeUp];
        for (int i = 0; i < sizeUp; i++)
        {
            upvalues[i] = new LuaUpvalue(Ar);
        }
        return upvalues;
    }

    private static void MapOpcodes(byte[] code)
    {
        for (int i = 0; i < code.Length; i += 4)
        {
            uint instr = BitConverter.ToUInt32(code, i);
            byte opcode = (byte)(instr & 0x7F);
            byte mapped = _opcodeMapping[opcode];

            instr = (instr & ~0x7Fu) | (uint)(mapped & 0x7F);
            Array.Copy(BitConverter.GetBytes(instr), 0, code, i, 4);
        }
    }
}

public class LuaConstant
{
    public byte Type;
    public byte[] Data = [];
    public string StrData = string.Empty;

    public LuaConstant(FNRCLuaArchive Ar)
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
                StrData = Ar.ReadNRCString();
                break;
        }
    }
}

public class LuaUpvalue(FNRCLuaArchive Ar)
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

    public LuaDebug(FNRCLuaArchive Ar)
    {
        SizeLineInfo = Ar.ReadLuaInt();
        LineInfo = Ar.ReadBytes((int)SizeLineInfo);
        AbsLineInfo = Ar.ReadLuaArray(() => new LuaAbsLineInfo(Ar));
        LocVars = Ar.ReadLuaArray(() => new LuaLocVar(Ar));
        UpvalueNames = Ar.ReadLuaArray(() => new LuaUpvalueName(Ar));
    }
}

public class LuaAbsLineInfo(FNRCLuaArchive Ar)
{
    public ulong Pc = Ar.ReadLuaInt();
    public ulong Line = Ar.ReadLuaInt();
}

public class LuaLocVar(FNRCLuaArchive Ar)
{
    public string NameData = Ar.ReadNRCString();
    public ulong StartPc = Ar.ReadLuaInt();
    public ulong EndPc = Ar.ReadLuaInt();
}

public class LuaUpvalueName(FNRCLuaArchive Ar)
{
    public string NameData = Ar.ReadNRCString();
}

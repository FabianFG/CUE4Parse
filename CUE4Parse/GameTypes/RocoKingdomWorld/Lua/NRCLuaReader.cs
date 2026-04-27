using System;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Lua;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Lua;

public class FNRCLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLuaArchive(name, data, versions)
{
    // Strings are encrypted
    public override string ReadLuaString()
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

public class NRCLuaReader
{
    private static readonly byte[] _opcodeMapping = [
        0, 66, 70, 55, 20, 17, 73, 28, 46, 68, 52, 42, 43, 19, 77, 21,
        35, 81, 8, 13, 7, 18, 79, 6, 59, 60, 67, 64, 10, 24, 39, 34,
        31, 53, 75, 58, 37, 3, 26, 72, 5, 51, 23, 9, 74, 76, 36, 71,
        63, 56, 65, 25, 57, 62, 22, 32, 12, 40, 33, 38, 2, 47, 44, 69,
        1, 49, 48, 50, 30, 45, 27, 78, 16, 80, 11, 54, 4, 41, 15, 14,
        61, 29, 82
    ];

    public static LuaBytecode ReadBytecode(FNRCLuaArchive Ar)
    {
        var header = FLuaReader.ReadHeader(Ar);
        header.Version = 0x54; // Header is standard except the version

        var result = new LuaBytecode
        {
            Header = header,
            MainFunc = ReadFunction(Ar)
        };

        return result;
    }

    private static LuaFunction ReadFunction(FNRCLuaArchive Ar)
    {
        var f = new LuaFunction
        {
            SourceName = Ar.ReadLuaString(),
            LineDefined = Ar.ReadLuaInt(),
            Code = [.. Ar.ReadLuaArray(() => Ar.ReadBytes(4)).SelectMany(x => x)],
            LastLineDefined = Ar.ReadLuaInt(), // Shuffled
            NumParams = Ar.Read<byte>(), // Shuffled
            Constants = Ar.ReadLuaArray(() => FLuaReader.ReadConstant(Ar)),
            IsVarArg = Ar.Read<byte>() // Shuffled
        };

        f.Upvalues = ReadUpvalues(Ar, f);
        f.Protos = Ar.ReadLuaArray(() => ReadFunction(Ar));
        f.Debug = FLuaReader.ReadDebug(Ar);

        MapOpcodes(f.Code); // Opcode is shuffled

        return f;
    }

    private static LuaUpvalue[] ReadUpvalues(FNRCLuaArchive Ar, LuaFunction f)
    {
        int sizeUp = (int) Ar.ReadLuaInt();
        Ar.Read<byte>(); // Shuffled
        f.MaxStackSize = 255; // Read above, but I had to increase it manually for some reason, something else might be wrong?
        var upvalues = new LuaUpvalue[sizeUp];
        for (int i = 0; i < sizeUp; i++)
        {
            upvalues[i] = FLuaReader.ReadUpvalue(Ar);
        }
        return upvalues;
    }

    private static void MapOpcodes(byte[] code)
    {
        for (int i = 0; i < code.Length; i += 4)
        {
            uint instr = BitConverter.ToUInt32(code, i);
            byte opcode = (byte) (instr & 0x7F);
            byte mapped = _opcodeMapping[opcode];

            instr = (instr & ~0x7Fu) | (uint) (mapped & 0x7F);
            Array.Copy(BitConverter.GetBytes(instr), 0, code, i, 4);
        }
    }
}

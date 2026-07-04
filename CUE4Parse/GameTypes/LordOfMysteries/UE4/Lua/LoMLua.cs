using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;

namespace CUE4Parse.GameTypes.LordOfMysteries.UE4.Lua;

public static class LoMLua
{
    private static readonly byte[] _xorKey = Encoding.ASCII.GetBytes("c7fjs-432890fadnsyu9reqwj;lerwqio;jf;ldsanmdgmzz"); // off_14C7ECAE8

    public static byte[] DecryptLuaJITBytecode(string name, byte[] data)
    {
        if (data is not [0x1B, 0x4C, 0x4A, 0x82, ..])
            return data;

        data[3] = (byte) ELuaJITVersion.LuaJit21; // Custom LuaJIT version 0x82 -> standard LuaJIT version

        using var Ar = new FLuaJITArchive(name, data) { Position = 4 };
        var flags = Ar.ReadUleb128();
        if ((flags & FLuaJIT.BCDumpFlagStrip) is 0)
            Ar.ReadLuaString(); // Not encrypted

        while (Ar.Position < Ar.Length)
        {
            var protoLength = Ar.ReadUleb128();
            if (protoLength is 0)
                break;
            if (protoLength > Ar.Length - Ar.Position)
                throw new ParserException($"Invalid LuaJIT prototype length in \"{name}\"");

            var protoEnd = Ar.Position + protoLength;
            var proto = data.AsSpan((int) Ar.Position, protoLength);
            for (var i = 0; i < proto.Length; i++)
                proto[i] ^= _xorKey[i % _xorKey.Length];

            RemapOpcodes(Ar, proto);
            Ar.Position = protoEnd;
        }

        return data;
    }

    private static void RemapOpcodes(FLuaJITArchive Ar, Span<byte> proto)
    {
        var protoPosition = Ar.Position;
        Ar.Position += 4; // flags, numparams, framesize, numuv
        Ar.ReadUleb128(); // kgc
        Ar.ReadUleb128(); // kn
        var bytecodeCount = Ar.ReadUleb128();
        var debugInfoSize = Ar.ReadUleb128();

        if (debugInfoSize != 0)
        {
            Ar.ReadUleb128(); // first line
            Ar.ReadUleb128(); // line count
        }

        var bytecodeSize = bytecodeCount * 4;
        var bytecodeOffset = (int) (Ar.Position - protoPosition);
        if (bytecodeSize > proto.Length - bytecodeOffset)
            throw new ParserException("Invalid LuaJIT bytecode instruction stream");

        for (var i = 0; i < bytecodeCount; i++)
        {
            var instructionOffset = bytecodeOffset + i * 4;
            proto[instructionOffset] = RemapOpcode(proto[instructionOffset]); // Shuffled opcode as always
        }
    }

    private static byte RemapOpcode(byte opcode)
    {
        return opcode switch
        {
            <= 0x11 => opcode,
            >= 0x12 and <= 0x18 => (byte) (opcode + 0x1B), // UGET..FNEW
            >= 0x19 and <= 0x1F => (byte) (opcode - 0x07), // MOV..MULVN
            0x20 => 0x1A, // MODVN
            0x21 => 0x19, // DIVVN
            >= 0x22 and <= 0x24 => (byte) (opcode - 0x07), // ADDNV..MULNV
            0x25 => 0x1F, // MODNV
            0x26 => 0x1E, // DIVNV
            >= 0x27 and <= 0x29 => (byte) (opcode - 0x07), // ADDVV..MULVV
            0x2A => 0x24, // MODVV
            0x2B => 0x23, // DIVVV
            >= 0x2C and <= 0x33 => (byte) (opcode - 0x07), // POW..KNIL
            >= 0x34 and <= 0x40 => opcode,
            >= 0x41 and <= 0x44 => (byte) (opcode + 0x08), // RETM..RET1
            >= 0x45 and <= 0x4C => (byte) (opcode - 0x04), // CALLM..ISNEXT
            0x4D => 0x4F, // FORL
            0x4E => 0x50, // IFORL
            0x4F => 0x51, // JFORL
            0x50 => 0x4D, // FORI
            0x51 => 0x4E, // JFORI
            0x52 => 0x52, // ITERL
            0x53 => 0x53, // IITERL
            0x54 => 0x54, // JITERL
            0x55 => 0x58, // JMP
            0x56 => 0x55, // LOOP
            0x57 => 0x56, // ILOOP
            0x58 => 0x57, // JLOOP
            >= 0x59 and <= 0x60 => opcode,
            _ => throw new ParserException($"Invalid Lord of Mysteries LuaJIT opcode 0x{opcode:X2}")
        };
    }
}

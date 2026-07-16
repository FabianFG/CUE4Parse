namespace CUE4Parse.UE4.Lua.Readers;

// https://github.com/LuaJIT/LuaJIT/blob/a2bde60819d83e6f75130ac2c93ee4b3c7615800/src/lj_bcdump.h#L36
// Standard LuaJIT versions, if set to 0x80 or higher then bytecode is modified
public enum ELuaJITVersion
{
    LuaJit20 = 0x01,
    LuaJit21 = 0x02,
}

public static class FLuaJIT
{
    // https://github.com/LuaJIT/LuaJIT/blob/a2bde60819d83e6f75130ac2c93ee4b3c7615800/src/lj_bcdump.h#L41
    public const int BCDumpFlagStrip = 0x02;

    public static bool IsValidLuaJITMagic(ReadOnlySpan<byte> data) => data is [0x1B, 0x4C, 0x4A, ..]; // LuaJIT magic "\1BLJ"
}

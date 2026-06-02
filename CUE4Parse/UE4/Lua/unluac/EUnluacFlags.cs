using System;

namespace CUE4Parse.UE4.Lua.unluac;

// match custom unluac native flags
[Flags]
public enum EUnluacFlags
{
    None           = 0,
    RawString      = 1 << 0,
    Luaj           = 1 << 1,
    NoDebug        = 1 << 2,

    OpCodeMap      = 1 << 16,
    OpCodeMapPatch = 1 << 17,

    Decompile      = 1 << 24,
    Disassemble    = 1 << 25,
}

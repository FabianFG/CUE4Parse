using System.Text;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Tencent.ValorantSource.Lua;

public class ValorantSourceLua
{
    public class FValorantSourceLuaArchive(string name, byte[] data, VersionContainer? versions = null) : FLua54Archive(name, data, versions)
    {
        private readonly byte[] _xorKey =
        [
            0x11, 0x21, 0x36, 0x47, 0x46, 0x57, 0xA7, 0x8D,
            0x9D, 0x84, 0x90, 0xD8, 0xAB, 0x00, 0x8C, 0x35,
            0x26, 0x1A, 0xF7, 0xE4, 0x58, 0x05, 0xB8, 0xB3,
            0x15, 0x07, 0xD0, 0x2C, 0x1E, 0x8F, 0xF6, 0xC8
        ];

        // Strings are encrypted
        public override string ReadLuaString()
        {
            var size = ReadLuaInt();
            if (size <= 1)
                return string.Empty;

            var length = (int) size - 1;
            var b = ReadBytes(length);

            for (int i = 0; i < length; i++)
            {
                b[i] = (byte) (b[i] ^ _xorKey[i % _xorKey.Length]);
            }

            return Encoding.UTF8.GetString(b);
        }
    }

    private static readonly Dictionary<byte, byte> _opcodeMapping = new()
    {
        [0] = 21,  // ADDI
        [1] = 22,  // ADDK
        [2] = 23,  // SUBK
        [3] = 24,  // MULK
        [4] = 25,  // MODK
        [5] = 26,  // POWK
        [6] = 27,  // DIVK
        [7] = 28,  // IDIVK
        [8] = 29,  // BANDK
        [9] = 30,  // BORK
        [10] = 31, // BXORK
        [11] = 32, // SHRI
        [12] = 33, // SHLI
        [13] = 34, // ADD
        [14] = 35, // SUB
        [15] = 36, // MUL
        [16] = 37, // MOD
        [17] = 38, // POW
        [18] = 39, // DIV
        [19] = 40, // IDIV
        [20] = 41, // BAND
        [21] = 42, // BOR
        [22] = 43, // BXOR
        [23] = 44, // SHL
        [24] = 45, // SHR
        [25] = 46, // MMBIN
        [26] = 47, // MMBINI
        [27] = 48, // MMBINK
        [28] = 49, // UNM
        [29] = 50, // BNOT
        [30] = 51, // NOT
        [31] = 52, // LEN
        [32] = 53, // CONCAT
        [33] = 0,  // MOVE
        [34] = 1,  // LOADI
        [35] = 2,  // LOADF
        [36] = 3,  // LOADK
        [37] = 4,  // LOADKX
        [38] = 5,  // LOADFALSE
        [39] = 6,  // LFALSESKIP
        [40] = 7,  // LOADTRUE
        [41] = 8,  // LOADNIL
        [42] = 9,  // GETUPVAL
        [43] = 10, // SETUPVAL
        [44] = 11, // GETTABUP
        [45] = 12, // GETTABLE
        [46] = 13, // GETI
        [47] = 14, // GETFIELD
        [48] = 15, // SETTABUP
        [49] = 16, // SETTABLE
        [50] = 17, // SETI
        [51] = 18, // SETFIELD
        [52] = 19, // NEWTABLE
        [53] = 20, // SELF
        [54] = 54, // CLOSE
        [55] = 55, // TBC
        [56] = 56, // JMP
        [57] = 57, // EQ
        [58] = 58, // LT
        [59] = 59, // LE
        [60] = 60, // EQK
        [61] = 61, // EQI
        [62] = 62, // LTI
        [63] = 63, // LEI
        [64] = 64, // GTI
        [65] = 65, // GEI
        [66] = 66, // TEST
        [67] = 67, // TESTSET
        [68] = 68, // CALL
        [69] = 69, // TAILCALL
        [70] = 70, // RETURN
        [71] = 71, // RETURN0
        [72] = 72, // RETURN1
        [73] = 73, // FORLOOP
        [74] = 74, // FORPREP
        [75] = 75, // TFORPREP
        [76] = 76, // TFORCALL
        [77] = 77, // TFORLOOP
        [78] = 78, // SETLIST
        [79] = 79, // CLOSURE
        [80] = 81, // VARARGPREP
        [81] = 80, // VARARG
        [82] = 82, // EXTRAARG
    };

    public static byte[] DecryptLuaBytecode(string name, byte[] bytes)
    {
        var Ar = new FValorantSourceLuaArchive(name, bytes);
        using var msOut = new MemoryStream();
        using var writer = new FLua54ArchiveWriter(msOut);
        FLuaWriter54.Write(writer, FLua54Reader.ReadLuaBytecode(Ar, _opcodeMapping));
        writer.Flush();

        return msOut.ToArray();
    }
}

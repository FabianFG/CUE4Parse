using Berrysoft.XXTea;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;

namespace CUE4Parse.GameTypes.NetEase.RacingMaster.Lua;

public static class RacingMasterLua
{
    private static readonly byte[] _key = "15e6229e8e23dba6"u8.ToArray(); // Full key is 15e6229e8e23dba6328e91e2bac430c6
    private static ReadOnlySpan<byte> _encryptedLuaMagic => "SG17"u8;

    private static readonly byte[] _opcodeTable =
    [
        19, 8, 1, 2, 3, 4, 5, 6, 7, 81, 9, 10, 13, 14, 15, 16,
        12, 11, 17, 18, 66, 20, 77, 0, 21, 22, 23, 24, 25, 26, 27, 28,
        29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44,
        45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
        61, 62, 63, 64, 65, 68, 69, 70, 67, 76, 71, 72, 73, 74, 75, 78,
        79, 80, 82
    ];
    private static readonly Dictionary<byte, byte> _opcodeMapping = _opcodeTable.ToOpcodeMapping();

    public static byte[] DecryptLuaBytecode(string path, byte[] data)
    {
        if (!data.AsSpan().StartsWith(_encryptedLuaMagic))
            return data;

        var decryptor = new XXTeaCryptor();
        byte[] decrypted = [.. decryptor.Decrypt(data.AsSpan(4), _key)];
        if (!FLuaReader.IsValidLuaMagic(decrypted))
            throw new ParserException($"Failed to decrypt Racing Master lua {path}");

        using var Ar = new FLua54Archive(path, decrypted);
        return new FLuaWriter54(FLua54Reader.ReadLuaBytecode(Ar, _opcodeMapping)).GetBuffer();
    }
}

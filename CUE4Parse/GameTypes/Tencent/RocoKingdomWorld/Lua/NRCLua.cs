using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;
using CUE4Parse.UE4.Lua.Archives;
using CUE4Parse.UE4.Lua.Readers;
using CUE4Parse.UE4.Lua.Writers;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Lua;

public static class NRCLua
{
    // Password found in NRC/Plugins/NRCCrypto/Config/Crypto.ini
    private static ReadOnlySpan<byte> Password => "UhpQKQT4xj+VZCY74SQd7klOeZDtW3d1YN6MAZLDgcc="u8;
    private static readonly byte[] _key = DeriveKey();
    private static readonly byte[] _iv = DeriveIV();

    private static byte[] DeriveKey()
        => Rfc2898DeriveBytes.Pbkdf2(Password, SHA256.HashData(Password), 100000, HashAlgorithmName.SHA256, 32);

    private static byte[] DeriveIV()
    {
        var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Password);
        hash.AppendData("iv"u8);
        return hash.GetHashAndReset()[..16];
    }

    public static byte[] Decrypt(this byte[] encrypted, int offset = 0)
    {
        using var aes = Aes.Create();

        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, offset, encrypted.Length - offset);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct LuaMLEHeader
    {
        public uint Magic;
        public byte Version;
        public ushort BlockSize;
        public int BlockCount;
        public ushort LastBlockSize;
        public ulong KeySeed;
    }

    private static byte[] DecryptLuaData(string name, byte[] encryptedData)
    {
        Span<byte> encryptedSpan = encryptedData.AsSpan();
        if (encryptedSpan.Length > 4 && encryptedSpan[..4].SequenceEqual("\x1BMLE"u8))
        {
            LuaMLEHeader header = MemoryMarshal.Read<LuaMLEHeader>(encryptedSpan);
            ReadOnlySpan<ulong> key =
            [
                0x469138BA604C283B,
                0xB1C68D6BAC512C34,
                0xFBA953A7AFEE36DE,
                0xB2C38A4053060DAD
            ];

            var plaintext = new byte[header.BlockCount * header.BlockSize + header.LastBlockSize];
            FConfigurableCryptoAlgorithmMLE.DecryptSlice(
                encryptedSpan[Unsafe.SizeOf<LuaMLEHeader>()..],
                MemoryMarshal.AsBytes(key), plaintext,
                header.BlockSize, header.KeySeed);

            return plaintext;
        }

        if (encryptedSpan.Length > 8 && encryptedSpan[..3].SequenceEqual("\xFA\xE5\xC0"u8))
        {
            return encryptedData.Decrypt(7);
        }

        return encryptedData.Decrypt();
    }

    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData)
    {
        var decryptedData = DecryptLuaData(name, encryptedData);
        return decryptedData;

        if (!FLuaReader.IsValidLuaMagic(decryptedData))
            throw new InvalidDataException("Failed to decrypt. Expected Lua magic");

        using var Ar = new FNRCLuaArchive(name, decryptedData, null);
        var lua = NRCLuaReader.ReadBytecode(Ar);

        using var msOut = new MemoryStream();
        using (var writer = new FLua54ArchiveWriter(msOut))
        {
            FLuaWriter54.Write(writer, lua);
            writer.Flush();
        }

        return msOut.ToArray();
    }
}

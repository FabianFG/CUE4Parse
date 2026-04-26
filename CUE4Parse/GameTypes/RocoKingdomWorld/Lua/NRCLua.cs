using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.UE4.Lua;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Lua;

public static class NRCLua
{
    // Password found in NRC/Plugins/NRCCrypto/Config/Crypto.ini
    private static readonly byte[] _password = Encoding.ASCII.GetBytes("UhpQKQT4xj+VZCY74SQd7klOeZDtW3d1YN6MAZLDgcc=");
    private static readonly byte[] _key = DeriveKey();
    private static readonly byte[] _IV = DeriveIV();

    private static byte[] DeriveKey()
    {
        var salt = SHA256.HashData(_password);
        using var pbkdf2 = new Rfc2898DeriveBytes(_password, salt, 100000, HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32);
    }

    private static byte[] DeriveIV() => [.. SHA256.HashData([.. _password, .. Encoding.ASCII.GetBytes("iv")]).Take(16)]; // Bruh

    public static byte[] Decrypt(this byte[] encrypted)
    {
        using var aes = Aes.Create();

        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _key;
        aes.IV = _IV;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }

    public static byte[] DecryptLuaBytecode(string name, byte[] encryptedData)
    {
        var decryptedData = encryptedData.Decrypt();
        using var Ar = new FNRCLuaArchive(name, decryptedData, null);
        var lua = new LuaBytecode(Ar);

        using var msOut = new MemoryStream();
        using (var writer = new FLuaArchiveWriter(msOut))
        {
            NRCLuaWriter.Write(writer, lua);
            writer.Flush();
        }

        return msOut.ToArray();
    }
}

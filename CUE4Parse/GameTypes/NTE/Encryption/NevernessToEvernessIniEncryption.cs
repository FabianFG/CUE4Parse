using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CUE4Parse.GameTypes.NTE.Encryption;

// Reversed by Shiragasane
// CN key provided by formagGino
public static class NevernessToEvernessIniEncryption
{
    private const string SPLIT_TOKEN = "|SPLIT|";
    private static readonly byte[][] _potentialKeys =
    [
        Encoding.ASCII.GetBytes("UVbP6pjjw5KZhvddie3tfhg1pVkkveY8"), // GLB
        Encoding.ASCII.GetBytes("1zh6IOlIohrR88UNPjiLisrkWACUQYuz"), // CN
    ];

    private static byte[] _activeKey = [];

    public static byte[] DecryptIni(byte[] data, int requestedSize)
    {
        if (data == null || data.Length == 0)
            return [];

        var inputData = data.Length > requestedSize ? data[..requestedSize] : data;
        var text = Encoding.UTF8.GetString(inputData).TrimStart('\uFEFF'); // Trimming Byte Order Mask
        if (string.IsNullOrWhiteSpace(text))
            return data;

        var result = new StringBuilder();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        using Aes aes = Aes.Create();
        foreach (var line in lines.Select(l => l.Trim()))
        {
            if (!TryDecryptLine(line, aes, out string decrypted))
            {
                result.AppendLine(line); // Because lines might not be encrypted
                continue;
            }

            foreach (var part in decrypted.Split(SPLIT_TOKEN, StringSplitOptions.RemoveEmptyEntries))
            {
                result.AppendLine(part);
            }
        }

        return Encoding.UTF8.GetBytes(result.ToString());
    }

    private static bool TryDecryptLine(string line, Aes aes, out string decrypted)
    {
        decrypted = string.Empty;

        var buffer = new byte[(line.Length * 3) / 4 + 4]; // Max base64 decode
        if (!Convert.TryFromBase64String(line, buffer, out int len) || len == 0 || len % 16 != 0)
            return false;
        if (_activeKey.Length > 0)
            return TryDecrypt(buffer, len,aes, _activeKey, out decrypted);

        // Key depends on what version of the game we are on, so to make it simple let's just try all potential keys
        foreach (var key in _potentialKeys)
        {
            if (TryDecrypt(buffer, len, aes, key, out decrypted))
            {
                _activeKey = key;
                return true;
            }
        }

        throw new CryptographicException("No valid key found to decrypt .ini file");
    }

    private static bool TryDecrypt(byte[] data, int len, Aes aes, byte[] key, out string decrypted)
    {
        try
        {
            aes.Key = key;
            var bytes = aes.DecryptEcb(data.AsSpan(0, len), PaddingMode.PKCS7);
            decrypted = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            decrypted = string.Empty;
            return false;
        }
    }
}

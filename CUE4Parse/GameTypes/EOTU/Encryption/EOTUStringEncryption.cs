using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.EOTU.Encryption;

public class EOTUStringEncryption
{
    private const string Suffix = "ENCRYPTED";
    private const string Key = "BM8c/12uWU7Z78MbVza5A6MrCeoTIaS6nIbXjFQNlrs=";

    public static string DecryptString(FArchive Ar) => DecryptString(Ar.ReadFString());
    public static string DecryptString(string encryptedString)
    {
        if (string.IsNullOrEmpty(encryptedString) || !encryptedString.EndsWith(Suffix, StringComparison.Ordinal))
            return encryptedString;

        var payload = encryptedString[..^Suffix.Length];
        var data = Convert.FromBase64String(payload);
        var keyBytes = Encoding.ASCII.GetBytes(Key);

        for (int i = 0; i < data.Length; i++)
            data[i] ^= keyBytes[i % keyBytes.Length];

        return Encoding.Unicode.GetString(data).TrimEnd('\0');
    }
}

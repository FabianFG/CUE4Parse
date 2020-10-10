using System.Security.Cryptography;

namespace CUE4Parse.Encryption.Aes
{
    public static class Aes
    {
        public const int ALIGN = 16;
        public const int BLOCK_SIZE = 16 * 8;

        private static readonly AesCryptoServiceProvider Provider;
        public static byte[] Decrypt(this byte[] encrypted, FAesKey key)
        {
            return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        static Aes()
        {
            Provider = new AesCryptoServiceProvider();
            Provider.Mode = CipherMode.ECB;
            Provider.Padding = PaddingMode.None;
            Provider.BlockSize = BLOCK_SIZE;
        }
    }
}
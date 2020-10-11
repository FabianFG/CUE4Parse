using System;
using CUE4Parse.Utils;

namespace CUE4Parse.Encryption.Aes
{
    public class FAesKey
    {
        public readonly byte[] Key;
        public readonly string KeyString;

        public FAesKey(byte[] key)
        {
            if (key.Length != 32)
                throw new ArgumentException("Aes Key must be 32 bytes long");
            Key = key;
            KeyString = "0x" + BitConverter.ToString(key);
        }

        public FAesKey(string keyString)
        {
            if (!keyString.StartsWith("0x"))
                keyString = "0x" + keyString;
            if (keyString.Length != 66)
                throw new ArgumentException("Aes Key must be 32 bytes long");
            KeyString = keyString;
            Key = keyString.Substring(2).ParseHexBinary();
        }

        public override string ToString() => KeyString;
    }
}
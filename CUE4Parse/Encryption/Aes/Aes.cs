using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.Encryption.Aes
{
    public static class Aes
    {
        public const int ALIGN = 16;
        public const int BLOCK_SIZE = 16 * 8;

        private static readonly AesProvider Provider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decrypt(this byte[] encrypted, FAesKey key)
        {
            return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decrypt(this byte[] encrypted, int beginOffset, int count, FAesKey key)
        {
            return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, beginOffset, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecryptApexMobile(this byte[] encrypted, FAesKey key)
        {
            return ApexDecryptData(encrypted, 0, encrypted.Length, key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecryptApexMobile(this byte[] encrypted, int beginOffset, int count, FAesKey key)
        {
            return ApexDecryptData(encrypted, beginOffset, count, key);
        }

        private static readonly byte[] _table1 =
        {
            0x20, 0xDB, 0x4B, 0x46, 0xEE, 0xF, 0xBC, 0xAC, 0x43, 0x32, 0xB1, 0x1E, 0xB, 0x7C, 0x6E, 0x9A, 6, 0x55,
            0xBB, 0xA6, 0xFA, 0x89, 0x41, 0x63, 0x23, 0xA8, 0xCE, 1, 0x53, 0xDA, 0x87, 0x78, 0x2E, 0x81, 0x49, 0x70,
            0x28, 0xCF, 0x60, 0x2B, 0x40, 0x9C, 0x5F, 0x1C, 0x44, 0x39, 0x4D, 0xC1, 0x21, 0x3A, 0xCC, 0xB4, 0x94,
            0xAF, 0x5E, 0xAE, 0x31, 0x6F, 0x4C, 0xEC, 0x9D, 0xF8, 0xD4, 0x54, 0xFE, 0x1A, 0x12, 0xCB, 0xF4, 0x8E,
            0x7B, 0xEB, 0xD1, 0x34, 0x83, 0x50, 0x18, 0x64, 0xE5, 0xEA, 0xB3, 0xFB, 0x6D, 0xDC, 0xE, 0x66, 0x3D,
            0x2D, 0xE4, 0xE1, 0x6A, 0x1B, 0xF6, 0xF0, 0x3C, 0xD, 0x22, 0xC2, 0x5C, 0x51, 0xBA, 0x86, 0xF5, 0x48,
            0x71, 0xCD, 0xEF, 0x27, 0xE9, 0xD3, 0xDF, 0xA2, 0xE2, 0xFD, 0x3E, 0x73, 0x90, 0xC0, 0x68, 0, 0xFF, 0xB8,
            0x7F, 0x82, 0x8A, 0x95, 0xB2, 0x2F, 0x1D, 0x5A, 0x17, 0x80, 0x74, 0x9E, 0x7D, 0xA, 0xE3, 0x6C, 0x72,
            0x45, 0x24, 0x5B, 0xA9, 0xFC, 0x84, 0x36, 0x37, 0xA7, 0x8B, 0xDD, 0x16, 0x56, 0xC8, 0xC9, 0x13, 0x42,
            0xE6, 0xC4, 8, 0xA1, 0x98, 0x1F, 0x29, 0x26, 0xC6, 0x77, 0x35, 0x79, 0x38, 0xF9, 0xB5, 0x3F, 0xD8, 0x6B,
            0x8D, 0x99, 0xAB, 0x97, 0xF3, 0x69, 0xAD, 0xD2, 0x91, 0xE7, 0x9B, 0xB0, 0xA5, 0x93, 0xA0, 0x76, 0x7E,
            0x19, 0xE0, 0x3B, 0xF2, 0x2C, 9, 0xD5, 0xBF, 0xBE, 0xA4, 0x2A, 0x8F, 0x11, 0x4A, 0xD0, 0x58, 0x61, 0x5D,
            0x10, 0xED, 0xCA, 0xB6, 0x57, 0x67, 0xD6, 0xC3, 0xA3, 0xD7, 0xBD, 0x52, 0xAA, 0x7A, 0x47, 0x33, 0x62,
            0xF1, 0xC7, 0x65, 4, 0x25, 0x75, 0xE8, 0x15, 0xC, 0x88, 0x85, 0x96, 5, 2, 0x92, 0xF7, 3, 0xC5, 0x4E,
            0x8C, 0xD9, 0x9F, 0xB9, 0x4F, 0xB7, 0xDE, 0x14, 0x59, 7, 0x30
        };

        private static readonly uint[] _table2 =
        {
            0x9EDEB, 0xD0456773, 0x7F8B6647, 0x9DC6722F, 0x895B671B, 0x55B8CC43, 0x238970D7, 0x9F477CEF,
            0x4D55284B, 0x8AB83693, 0x81571417, 0xB60BF4DF, 0x7857417B, 0xE597B33, 0x8025ADC7, 0x90E010CF,
            0x74A8521B, 0xFA6B7F83, 0xA35D27B7, 0x9854F89F, 0x87E21A6B, 0xAFFBDB23, 0x586603B7, 0x7684E99F,
            0xEB25AB2B, 0x2AA80D53, 0xDF395E67, 0x6B6DDFCF, 0x2898375B, 0x81DBACB3, 0xEE228217, 0xAC13E5FF
        };

        private static byte[] ApexDecryptData(byte[] encrypted, int beginOffset, int count, FAesKey key)
        {
            unsafe
            {
                Span<uint> rk = stackalloc uint[32];
                ApexSetupDecrypt(key, rk);

                var decrypt = new byte[count];
                Buffer.BlockCopy(encrypted, beginOffset, decrypt, 0, count);
                fixed (byte* pDecrypt = decrypt)
                {
                    for (var i = 0; i < count; i += 16)
                    {
                        ApexDecrypt(rk, pDecrypt + i);
                    }
                }

                return decrypt;
            }
        }

        private static unsafe void ApexSetupDecrypt(FAesKey key, Span<uint> rk)
        {
            Span<uint> internalKey = stackalloc uint[4];
            internalKey[0] = BitConverter.ToUInt32(key.Key).ByteSwap() ^ 0x1E3DFA;
            internalKey[1] = BitConverter.ToUInt32(key.Key, 4).ByteSwap() ^ 0x78F36777;
            internalKey[2] = BitConverter.ToUInt32(key.Key, 8).ByteSwap() ^ 0xD99D2CF;
            internalKey[3] = BitConverter.ToUInt32(key.Key, 12).ByteSwap() ^ 0x5E144852;
            for (var i = 0; i < 32; i++)
            {
                var tempInt = _table2[i] ^ internalKey[(i + 1) % 4] ^ internalKey[(i + 2) % 4] ^ internalKey[(i + 3) % 4];
                var temp = (byte*) &tempInt;
                var tablePart01 = _table1[temp[0]] | (uint) (_table1[temp[1]] << 8);
                var tablePart012 = tablePart01 | (uint) (_table1[temp[2]] << 16);
                var tableEntry = tablePart012 | (uint) (_table1[temp[3]] << 24);
                var rkValue = tableEntry ^ internalKey[i % 4] ^ (tableEntry >> 19 | tablePart012 << 13) ^ (tableEntry >> 9 | tablePart01 << 23);
                internalKey[i % 4] = rkValue;
                rk[i] = rkValue;
            }
        }

        private static unsafe void ApexDecrypt(Span<uint> rk, byte* data)
        {
            var src = (uint*) data;
            Span<uint> internalData = stackalloc uint[4];

            internalData[0] = src[0].ByteSwap();
            internalData[1] = src[1].ByteSwap();
            internalData[2] = src[2].ByteSwap();
            internalData[3] = src[3].ByteSwap();
            for (var i = 0; i < 32; i++)
            {
                var tempInt = rk[^(i + 1)] ^ internalData[(i + 1) % 4] ^ internalData[(i + 2) % 4] ^ internalData[(i + 3) % 4];
                var temp = (byte*) &tempInt;
                var tablePart3 = _table1[temp[3]];
                var tablePart0 = _table1[temp[0]];
                var tablePart01 = tablePart0 | (uint) (_table1[temp[1]] << 8);
                var tablePart012 = tablePart01 | (uint) (_table1[temp[2]] << 16);
                var tableEntry = tablePart012 | (uint) (tablePart3 << 24);
                internalData[i % 4] ^= tableEntry ^ (4 * tableEntry | (uint) (tablePart3 >> 6)) ^ (tableEntry >> 22 | tablePart012 << 10) ^ (tableEntry >> 14 | tablePart01 << 18) ^ (tableEntry >> 8 | (uint) tablePart0 << 24);
            }

            var intData = (uint*) data;
            intData[0] = internalData[3].ByteSwap();
            intData[1] = internalData[2].ByteSwap();
            intData[2] = internalData[1].ByteSwap();
            intData[3] = internalData[0].ByteSwap();
        }

        private static uint ByteSwap(this uint value) => value >> 24 | value >> 8 & 0xFF00 | value << 8 & 0xFF0000 | value << 24;

        static Aes()
        {
            Provider = AesProvider.Create();
            Provider.Mode = CipherMode.ECB;
            Provider.Padding = PaddingMode.None;
            Provider.BlockSize = BLOCK_SIZE;
        }
    }
}
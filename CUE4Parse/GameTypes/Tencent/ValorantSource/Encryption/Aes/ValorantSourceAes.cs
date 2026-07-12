using System.Buffers.Binary;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Crypto;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Tencent.ValorantSource.Encryption.Aes;

public static class ValorantSourceAes
{
    private const int RANDOM_STATE_SIZE = 624;
    public static readonly uint[] ZucXorTable = GenerateZuc128XorTable();
    public static readonly byte[] ZucXorTableBytes = [.. ZucXorTable.SelectMany(BitConverter.GetBytes)];

    public const ulong LOW_NIBBLES_MASK = 0x000000000F0F0F0F;
    public const ulong HIGH_NIBBLES_MASK = 0xFFFFFFFFF0F0F0F0;

    // In pak footer there's byte that determines cipher algorithm to use
    // I think other algorithm is unused but if it were to be used then it goes something like that:
    // key[9..12]  ^= EncryptionKeyGuid.A
    // key[1..4]   ^= EncryptionKeyGuid.B
    // key[17..20] ^= EncryptionKeyGuid.C
    // key[23..26] ^= EncryptionKeyGuid.D
    // and AES ECB decrypt using this key 0x984D74052F0E4C4AB00174A8106AFE8D92977E347E1CE5D99ACDFE3CB3B8E9A8, not per-pak RSA derived key
    public static byte[] ValorantSourceDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var key = (reader is PakFileReader { Info.CustomEncryptionData: { } pakKey } ? pakKey : reader.AesKey?.Key)
            ?? throw new NullReferenceException("reader.AesKey");

        var output = CUE4Parse.Encryption.Aes.Aes.Decrypt(bytes, beginOffset, count, new FAesKey(key));

        if (!isIndex) return output;

        for (var offset = 0; offset < output.Length; offset += sizeof(uint))
        {
            var keyIndex = (offset / sizeof(uint) + 2) & 0xF;
            var value = BinaryPrimitives.ReadUInt32LittleEndian(output.AsSpan(offset));
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(offset), value ^ ZucXorTable[keyIndex]);
        }

        return output;
    }

    // Result is:
    // 0x92, 0x8C, 0xF0, 0xD1, 0x1A, 0x5B, 0x85, 0x01,
    // 0x3A, 0x9F, 0x85, 0x14, 0x0F, 0xBE, 0x47, 0x0B,
    // 0xB6, 0x81, 0x5A, 0x17, 0xB8, 0x7C, 0xA6, 0xFC,
    // 0x7E, 0x03, 0xA9, 0xF2, 0xE5, 0xC4, 0xFC, 0x08,
    // 0xA5, 0xFF, 0x40, 0x3F, 0x01, 0x88, 0xCE, 0x46,
    // 0xD3, 0xD7, 0xD0, 0x94, 0x1A, 0x0F, 0xD5, 0x0A,
    // 0x5A, 0xA1, 0x15, 0xD3, 0xEC, 0xBD, 0x0C, 0x1A,
    // 0xC6, 0x06, 0xF1, 0xA2, 0x6E, 0x8A, 0x23, 0x40
    //
    // It can be simply taken from pak footer since they xor 0 values exposing the key
    // but this is how it is actually generated
    private static uint[] GenerateZuc128XorTable()
    {
        var state = new uint[RANDOM_STATE_SIZE * 2 + 1];

        state[0] = 164;
        for (var i = 1; i < RANDOM_STATE_SIZE; i++)
        {
            state[i] = (byte) (i - 33 * (state[i - 1] ^ state[i - 1] >> 6));
        }

        var previous = state[0];
        for (var i = 0; i < RANDOM_STATE_SIZE; i++)
        {
            var next = state[i + 1];
            var mixed = ((next & 0x80) | (previous & 0x7F)) >> 1;

            if ((previous & 1) != 0)
                mixed ^= 0x9908B0DF;

            state[RANDOM_STATE_SIZE + i] = mixed ^ state[i + 397];
            previous = next;
        }
        state[RANDOM_STATE_SIZE * 2] = RANDOM_STATE_SIZE;

        var key = new byte[16];
        var iv = new byte[16];
        for (var i = 0; i < key.Length; i++)
        {
            key[i] = NextRandomByte(state);
            iv[i] = NextRandomByte(state);
        }

        return Zuc128Engine.GenerateKeyStream(key, iv, 16);
    }

    private static byte NextRandomByte(uint[] state)
    {
        var index = (int) state[RANDOM_STATE_SIZE * 2];
        var value = state[index];
        state[RANDOM_STATE_SIZE * 2] = (uint) (index + 1);

        return (byte) (value ^ value << 7);
    }
}

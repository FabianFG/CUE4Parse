using System.Runtime.Intrinsics;
using CUE4Parse.GameTypes.FSR.Encryption.Aes;
using CUE4Parse.GameTypes.FunkoFusion.Encryption.Aes;
using CUE4Parse.GameTypes.PAXDEI.Encryption.Aes;
using CUE4Parse.GameTypes.Splitgate2.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;
using static System.Runtime.Intrinsics.X86.Aes;
using static System.Runtime.Intrinsics.Vector128;

namespace CUE4Parse.Encryption.Aes;

public static class CustomAesKeyExpansion
{
    private const int BlockSize = 16;

    public static void DecryptWithRoundKeys(byte[] input, int index, ReadOnlySpan<Vector128<byte>> roundKeys)
    {
        if (roundKeys.Length == 0) return;

        var state = Create(input, index);
        var rounds = roundKeys.Length - 1;
        state = Xor(state, roundKeys[0]);
        for (var i = 1; i < rounds; i++)
        {
            state = Decrypt(state, roundKeys[i]);
        }

        state = DecryptLast(state, roundKeys[rounds]);
        state.CopyTo(input, index);
    }

    public static byte[] DecryptWithRoundKeys(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if ((count & 0xF) != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var output = new byte[count];
        Array.Copy(bytes, beginOffset, output, 0, count);

        var roundKeys = KeyExpansion(reader);
        for (var i = 0; i < count; i += BlockSize)
        {
            DecryptWithRoundKeys(output, i, roundKeys);
        }

        return output;
    }

    private static Vector128<byte>[] KeyExpansion(IAesVfsReader reader)
    {
        return reader.Game switch
        {
            GAME_3on3FreeStyleRebound => FreeStyleReboundAes.RoundKeys,
            GAME_FunkoFusion => FunkoFusionAes.RoundKeys,
            GAME_PaxDei when reader.AesKey is not null => PaxDeiAes.KeyExpansion(reader.AesKey.Key),
            GAME_Splitgate2 or GAME_Empulse when reader.AesKey is not null => Aes1047Games.KeyExpansion(reader.AesKey.Key),
            _ => throw new NotSupportedException("Missing AES key.")
        };
    }
}

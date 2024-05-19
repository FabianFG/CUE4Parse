using System;
using System.Runtime.Intrinsics;
using CUE4Parse.UE4.VirtualFileSystem;
using static System.Runtime.Intrinsics.X86.Aes;
using static System.Runtime.Intrinsics.X86.Sse2;
using static System.Runtime.Intrinsics.Vector128;
using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.GameTypes.PAXDEI.Encryption.Aes;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public static class PaxDeiAes
{
    private static void Decrypt16(Span<byte> input, FAesKey aes, Span<byte> output)
    {
        var roundkeys = KeyExpansion(aes.Key);
        Vector128<byte> state = Create(input.ToArray());
        state = Xor(state, roundkeys[0]);
        for (var i = 1; i < 13; i++)
        {
            state = Decrypt(state, roundkeys[i]);
        }

        state = DecryptLast(state, roundkeys[13]);
        state.CopyTo(output);
    }

    public static byte[] PaxDeiDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var output = new byte[count];
        Array.Copy(bytes, beginOffset, output, 0, count);

        for (var i = 0; i < count / 16; i++)
        {
            Decrypt16(output.AsSpan(i * 16, 16), reader.AesKey, output.AsSpan(i * 16, 16));
        }

        return output;
    }

    public static Vector128<byte>[] KeyExpansion(byte[] key)
    {
        Vector128<byte>[] roundkeys =
        [
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero,
            Vector128<byte>.Zero
        ];
        var xmm5 = Create(key, 0).AsInt32();
        roundkeys[13] = xmm5.AsByte();
        var xmm1 = Create(key, 16).AsInt32();

        var xmm2 = ShiftLeftLogical128BitLane(xmm5, 4);
        var xmm3 = ShiftLeftLogical128BitLane(xmm1, 4);
        var xmm0 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm2 = Xor(xmm2, xmm5);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = KeygenAssist(xmm1.AsByte(), 1).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm3 = Xor(xmm3, xmm1);
        xmm3 = Xor(xmm3, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm3 = Xor(xmm3, xmm0);
        xmm1 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm0 = KeygenAssist(xmm2.AsByte(), 0).AsInt32();
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm3 = Xor(xmm3, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm1 = Xor(xmm1, xmm2);
        xmm1 = Xor(xmm1, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm1 = Xor(xmm1, xmm0);
        var xmm4 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm0 = KeygenAssist(xmm3.AsByte(), 2).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm1 = Xor(xmm1, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm4, 4);
        xmm4 = Xor(xmm4, xmm3);
        xmm4 = Xor(xmm4, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm4 = Xor(xmm4, xmm0);
        var xmm6 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm1.AsByte(), 2).AsInt32();
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm4 = Xor(xmm4, xmm0);
        xmm2 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm0 = xmm2;
        xmm2 = Xor(xmm2, xmm1);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm2 = Xor(xmm2, xmm0);
        var xmm7 = InverseMixColumns(xmm3.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm4.AsByte(), 0x4).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm2 = Xor(xmm2, xmm0);
        xmm3 = InverseMixColumns(xmm1.AsByte()).AsInt32();
        roundkeys[10] = xmm3.AsByte();
        xmm1 = xmm2;
        xmm0 = InverseMixColumns(xmm4.AsByte()).AsInt32();
        roundkeys[9] = xmm0.AsByte();
        xmm3 = ShiftLeftLogical128BitLane(xmm4, 4);
        xmm0 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        roundkeys[8] = xmm0.AsByte();
        xmm0 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm3 = Xor(xmm3, xmm4);
        xmm3 = Xor(xmm3, xmm0);
        xmm1 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm3 = Xor(xmm3, xmm0);
        xmm0 = KeygenAssist(xmm2.AsByte(), 0x3).AsInt32();
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm3 = Xor(xmm3, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm1 = Xor(xmm1, xmm2);
        xmm1 = Xor(xmm1, xmm0);
        xmm2 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm1 = Xor(xmm1, xmm0);
        xmm0 = KeygenAssist(xmm3.AsByte(), 0x8).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm1 = Xor(xmm1, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm2 = Xor(xmm2, xmm3);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm2 = Xor(xmm2, xmm0);
        xmm4 = InverseMixColumns(xmm3.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm1.AsByte(), 0x0).AsInt32();
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm2 = Xor(xmm2, xmm0);
        xmm3 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm0 = xmm3;
        roundkeys[7] = xmm4.AsByte();
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm3 = Xor(xmm3, xmm1);
        xmm3 = Xor(xmm3, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm3 = Xor(xmm3, xmm0);
        xmm4 = InverseMixColumns(xmm1.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm2.AsByte(), 0x10).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm3 = Xor(xmm3, xmm0);
        xmm1 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm0 = xmm1;
        roundkeys[6] = xmm4.AsByte();
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm1 = Xor(xmm1, xmm2);
        xmm1 = Xor(xmm1, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm1 = Xor(xmm1, xmm0);
        xmm4 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        roundkeys[5] = xmm4.AsByte();
        xmm4 = xmm3;
        xmm0 = KeygenAssist(xmm3.AsByte(), 0x0).AsInt32();
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm1 = Xor(xmm1, xmm0);
        xmm4 = ShiftLeftLogical128BitLane(xmm4, 4);
        xmm0 = xmm4;
        xmm4 = Xor(xmm4, xmm3);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm4 = Xor(xmm4, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm4 = Xor(xmm4, xmm0);
        xmm2 = InverseMixColumns(xmm3.AsByte()).AsInt32();
        roundkeys[4] = xmm2.AsByte();
        xmm2 = xmm1;
        xmm0 = KeygenAssist(xmm1.AsByte(), 0x20).AsInt32();
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm4 = Xor(xmm4, xmm0);
        xmm2 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm0 = xmm2;
        xmm2 = Xor(xmm2, xmm1);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm2 = Xor(xmm2, xmm0);
        xmm0 = ShiftLeftLogical128BitLane(xmm0, 4);
        xmm3 = InverseMixColumns(xmm1.AsByte()).AsInt32();
        xmm2 = Xor(xmm2, xmm0);
        roundkeys[3] = xmm3.AsByte();
        xmm3 = InverseMixColumns(xmm4.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm4.AsByte(), 0x0).AsInt32();
        roundkeys[2] = xmm3.AsByte();
        xmm3 = xmm4;
        xmm0 = Shuffle(xmm0, 0xAA);
        xmm2 = Xor(xmm2, xmm0);
        xmm3 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm0 = KeygenAssist(xmm2.AsByte(), 0x40).AsInt32();
        xmm1 = xmm3;
        xmm2 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm1 = ShiftLeftLogical128BitLane(xmm1, 4);
        roundkeys[1] = xmm2.AsByte();
        xmm3 = Xor(xmm3, xmm4);
        xmm3 = Xor(xmm3, xmm1);
        xmm1 = ShiftLeftLogical128BitLane(xmm1, 4);
        xmm0 = Shuffle(xmm0, 0xFF);
        xmm3 = Xor(xmm3, xmm1);
        xmm3 = Xor(xmm3, xmm0);
        roundkeys[0] = xmm3.AsByte();
        roundkeys[11] = xmm7.AsByte();
        roundkeys[12] = xmm6.AsByte();
        return roundkeys;
    }
}
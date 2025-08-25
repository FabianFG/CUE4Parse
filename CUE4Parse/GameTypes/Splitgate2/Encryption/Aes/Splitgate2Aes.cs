using System;
using System.Runtime.Intrinsics;
using CUE4Parse.UE4.VirtualFileSystem;
using static System.Runtime.Intrinsics.Vector128;
using static System.Runtime.Intrinsics.X86.Aes;
using static System.Runtime.Intrinsics.X86.Sse2;

namespace CUE4Parse.GameTypes.Splitgate2.Encryption.Aes;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public static class Splitgate2Aes
{
    private static void DecryptWithRoundKeys(byte[] input, int index, Vector128<byte>[] roundKeys)
    {
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

    public static byte[] Splitgate2Decrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var output = new byte[count];
        Array.Copy(bytes, beginOffset, output, 0, count);

        var roundKeys = KeyExpansion(reader.AesKey.Key);

        for (var i = 0; i < count / 16; i++)
        {
            DecryptWithRoundKeys(output, i * 16, roundKeys);
        }

        return output;
    }

    private static Vector128<byte>[] KeyExpansion(byte[] key)
    {
        Vector128<byte>[] roundKeys = new Vector128<byte>[14];

        Vector128<int> v6, v7, v8, v10, v11, v12, v13, v15, v16, v18, v19, v21;
        Vector128<int> v26, v27, v28, v29, v32, v33, v35, v36, v38, v39, v41;
        Vector128<int> v47, v48, v49, v50, v51, v53, v54, v56, v57, v59, v60;
        Vector128<int> v62, v63, v65, v66, v68, v73, v74, v75, v77, v78, v79, v80;
        Vector128<int> v83, v84, v86, v87, v89, v92;
        Vector128<int> xmm0, xmm3, xmm4, xmm5, xmm7, xmm8, xmm9, xmm10, xmm11, xmm12, xmm13, xmm14, xmm15;

        var xmm6 = Create(key, 0).AsInt32();
        var xmm2 = Create(key, 16).AsInt32();

        v6 = ShiftLeftLogical128BitLane(xmm6, 4);
        v7 = ShiftLeftLogical128BitLane(v6, 4);
        v8 = Xor(Xor(Xor(v6, xmm6), v7), ShiftLeftLogical128BitLane(v7, 4));

        xmm0 = KeygenAssist(xmm2.AsByte(), 1).AsInt32();
        v10 = Shuffle(xmm0, 0xFF);
        v11 = ShiftLeftLogical128BitLane(xmm2, 4);
        v12 = Xor(v11, xmm2);
        v13 = ShiftLeftLogical128BitLane(v11, 4);
        xmm3 = Xor(v8, v10);
        v15 = Xor(Xor(v12, v13), ShiftLeftLogical128BitLane(v13, 4));
        v16 = ShiftLeftLogical128BitLane(xmm3, 4);

        xmm0 = KeygenAssist(xmm3.AsByte(), 0).AsInt32();
        v18 = Shuffle(xmm0, 0xAA);
        v19 = ShiftLeftLogical128BitLane(v16, 4);
        xmm4 = Xor(v15, v18);
        v21 = Xor(Xor(Xor(v16, xmm3), v19), ShiftLeftLogical128BitLane(v19, 4));

        xmm0 = KeygenAssist(xmm4.AsByte(), 2).AsInt32();
        xmm7 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm5 = Xor(v21, Shuffle(xmm0, 0xFF));
        xmm8 = InverseMixColumns(xmm3.AsByte()).AsInt32();

        v26 = ShiftLeftLogical128BitLane(xmm4, 4);
        v27 = ShiftLeftLogical128BitLane(xmm5, 4);
        v28 = ShiftLeftLogical128BitLane(v26, 4);
        v29 = Xor(Xor(Xor(v26, xmm4), v28), ShiftLeftLogical128BitLane(v28, 4));

        xmm9 = InverseMixColumns(xmm4.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm5.AsByte(), 0).AsInt32();

        v32 = Shuffle(xmm0, 0xAA);
        v33 = ShiftLeftLogical128BitLane(v27, 4);
        xmm2 = Xor(v29, v32);

        v35 = ShiftLeftLogical128BitLane(xmm2, 4);
        v36 = Xor(Xor(Xor(v27, xmm5), v33), ShiftLeftLogical128BitLane(v33, 4));

        xmm0 = KeygenAssist(xmm2.AsByte(), 4).AsInt32();
        v38 = Shuffle(xmm0, 0xFF);
        v39 = ShiftLeftLogical128BitLane(v35, 4);
        xmm3 = Xor(v36, v38);

        v41 = Xor(Xor(Xor(v35, xmm2), v39), ShiftLeftLogical128BitLane(v39, 4));

        xmm10 = InverseMixColumns(xmm5.AsByte()).AsInt32();
        xmm11 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm12 = InverseMixColumns(xmm3.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm3.AsByte(), 0).AsInt32();

        xmm4 = Xor(v41, Shuffle(xmm0, 0xAA));

        v47 = ShiftLeftLogical128BitLane(xmm3, 4);
        v48 = Xor(v47, xmm3);
        v49 = ShiftLeftLogical128BitLane(v47, 4);
        v50 = ShiftLeftLogical128BitLane(xmm4, 4);
        v51 = Xor(Xor(v48, v49), ShiftLeftLogical128BitLane(v49, 4));

        xmm0 = KeygenAssist(xmm4.AsByte(), 8).AsInt32();
        v53 = Shuffle(xmm0, 0xFF);
        v54 = ShiftLeftLogical128BitLane(v50, 4);
        xmm2 = Xor(v51, v53);

        v56 = Xor(Xor(Xor(v50, xmm4), v54), ShiftLeftLogical128BitLane(v54, 4));
        v57 = ShiftLeftLogical128BitLane(xmm2, 4);
        xmm0 = KeygenAssist(xmm2.AsByte(), 0).AsInt32();
        v59 = Shuffle(xmm0, 0xAA);
        v60 = ShiftLeftLogical128BitLane(v57, 4);
        xmm3 = Xor(v56, v59);

        v62 = ShiftLeftLogical128BitLane(xmm3, 4);
        v63 = Xor(Xor(Xor(v57, xmm2), v60), ShiftLeftLogical128BitLane(v60, 4));
        xmm0 = KeygenAssist(xmm3.AsByte(), 0x10).AsInt32();
        v65 = Shuffle(xmm0, 0xFF);
        v66 = ShiftLeftLogical128BitLane(v62, 4);
        xmm5 = Xor(v63, v65);

        v68 = Xor(Xor(Xor(v62, xmm3), v66), ShiftLeftLogical128BitLane(v66, 4));

        xmm14 = InverseMixColumns(xmm3.AsByte()).AsInt32();
        xmm13 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm5.AsByte(), 0).AsInt32();

        xmm4 = Xor(v68, Shuffle(xmm0, 0xAA));

        v73 = ShiftLeftLogical128BitLane(xmm5, 4);
        v74 = ShiftLeftLogical128BitLane(v73, 4);
        v75 = Xor(Xor(Xor(v73, xmm5), v74), ShiftLeftLogical128BitLane(v74, 4));

        xmm0 = KeygenAssist(xmm4.AsByte(), 0x20).AsInt32();
        v77 = Shuffle(xmm0, 0xFF);
        v78 = ShiftLeftLogical128BitLane(xmm4, 4);
        v79 = Xor(v78, xmm4);
        v80 = ShiftLeftLogical128BitLane(v78, 4);

        xmm3 = Xor(v75, v77);

        xmm15 = InverseMixColumns(xmm4.AsByte()).AsInt32();

        v83 = Xor(Xor(v79, v80), ShiftLeftLogical128BitLane(v80, 4));
        v84 = ShiftLeftLogical128BitLane(xmm3, 4);
        xmm0 = KeygenAssist(xmm3.AsByte(), 0).AsInt32();
        v86 = Shuffle(xmm0, 0xAA);
        v87 = ShiftLeftLogical128BitLane(v84, 4);
        xmm2 = Xor(v83, v86);

        v89 = Xor(Xor(Xor(v84, xmm3), v87), ShiftLeftLogical128BitLane(v87, 4));

        xmm5 = InverseMixColumns(xmm5.AsByte()).AsInt32();
        xmm0 = KeygenAssist(xmm2.AsByte(), 0x40).AsInt32();

        v92 = Xor(v89, Shuffle(xmm0, 0xFF));

        xmm2 = InverseMixColumns(xmm2.AsByte()).AsInt32();
        xmm3 = InverseMixColumns(xmm3.AsByte()).AsInt32();

        roundKeys[0] = v92.AsByte();
        roundKeys[1] = xmm2.AsByte();
        roundKeys[2] = xmm3.AsByte();
        roundKeys[3] = xmm15.AsByte();
        roundKeys[4] = xmm5.AsByte();
        roundKeys[5] = xmm14.AsByte();
        roundKeys[6] = xmm13.AsByte();
        roundKeys[7] = xmm12.AsByte();
        roundKeys[8] = xmm11.AsByte();
        roundKeys[9] = xmm10.AsByte();
        roundKeys[10] = xmm9.AsByte();
        roundKeys[11] = xmm8.AsByte();
        roundKeys[12] = xmm7.AsByte();
        roundKeys[13] = xmm6.AsByte();

        return roundKeys;
    }
}

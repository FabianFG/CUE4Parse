using System;
using System.Reflection;
using System.Resources;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.UE4.Objects.UObject.FPackageFileSummary;

namespace CUE4Parse.GameTypes.ACE7.Encryption;

// https://github.com/atenfyr/UAssetAPI/blob/master/UAssetAPI/AC7Decrypt.cs
public class ACE7Decrypt
{
    private readonly byte[] fullKey;

    public ACE7Decrypt()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CUE4Parse.Resources.ACE7Key.bin");
        if (stream == null)
            throw new MissingManifestResourceException("Couldn't find ACE7Key.bin in Embedded Resources");
        fullKey = new byte[stream.Length];
        var _ = stream.Read(fullKey, 0, (int) stream.Length);
    }

    public FArchive DecryptUassetArchive(FArchive Ar, out ACE7XORKey key)
    {
        key = new ACE7XORKey(Ar.Name.SubstringBeforeLast('.').SubstringAfterLast('/'));
        var uasset = new byte[Ar.Length];
        var _ = Ar.Read(uasset, 0, (int) Ar.Length);
        return new FByteArchive(Ar.Name, DecryptUasset(uasset, key), Ar.Versions);
    }

    public FArchive DecryptUexpArchive(FArchive Ar, ACE7XORKey key)
    {
        var uexp = Ar.ReadBytes((int) Ar.Length);
        return new FByteArchive(Ar.Name, DecryptUexp(uexp, key), Ar.Versions);
    }

    public byte[] DecryptUasset(byte[] uasset, ACE7XORKey? key)
    {
        var arr = new byte[uasset.Length];
        BitConverter.GetBytes(PACKAGE_FILE_TAG).CopyTo(arr, 0);
        for (var i = 4; i < arr.Length; i++)
        {
            arr[i] = GetXORByte(uasset[i], ref key);
        }

        return arr;
    }

    public byte[] DecryptUexp(byte[] uexp, ACE7XORKey? key)
    {
        var arr = new byte[uexp.Length];
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = GetXORByte(uexp[i], ref key);
        }
        BitConverter.GetBytes(PACKAGE_FILE_TAG).CopyTo(arr, arr.Length - 4);
        return arr;
    }

    private byte GetXORByte(byte tagb, ref ACE7XORKey? xorkey)
    {
        if (xorkey == null) return tagb;
        tagb = (byte) ((uint) (tagb ^ fullKey[xorkey.pk1 * 1024 + xorkey.pk2]) ^ 0x77u);
        xorkey.pk1++;
        xorkey.pk2++;
        if (xorkey.pk1 >= 217) xorkey.pk1 = 0;
        if (xorkey.pk2 >= 1024) xorkey.pk2 = 0;
        return tagb;
    }
}
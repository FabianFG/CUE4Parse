using System;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Aion2.Objects;

public sealed class FAion2DatFileArchive(byte[] data, VersionContainer versions) : FByteArchive("Aion2DatFile", data, versions)
{
    private static readonly byte[] _xorKeyLong = [0x25, 0x00, 0xa8, 0x00, 0x7e, 0x00, 0x91, 0x00];
    private static readonly byte[] _xorKeyShort = [0x25, 0xa8, 0x7e, 0x91];

    public static void DecryptData(byte[] data) => DecryptData(data.AsSpan(), _xorKeyShort);
    public static void DecryptData(Span<byte> data, byte[] key)
    {
        if (data.Length <= 4 && key.Length == 4) return;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= key[i & key.Length - 1];
        }
    }

    private string ReadAion2String(byte[]? xorKey = null)
    {
        var length = Read<int>();
        if (length == 0)
            return string.Empty;

        var strLength = length > 0 ? length : -length * sizeof(ushort);
        if (strLength > Length - Position)
            throw new ParserException($"Invalid string length '{strLength}'");

        Span<byte> strBuffer = strLength <= 1024 ? stackalloc byte[strLength] : new byte[strLength];
        ReadExactly(strBuffer);

        if (xorKey is not null)
            DecryptData(strBuffer, xorKey);

        if (length > 0)
        {
            return Encoding.UTF8.GetString(strBuffer[..^1]);
        }
        else
        {
            if (xorKey is null) DecryptData(strBuffer, _xorKeyLong);
            return Encoding.Unicode.GetString(strBuffer[..^2]);
        }
    }

    public string ReadL10NString() => ReadAion2String(_xorKeyShort);
    public override string ReadFString() => ReadAion2String();
    public override FName ReadFName() => ReadFString();
}

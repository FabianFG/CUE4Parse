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

    public static void DecryptData(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= _xorKeyShort[i & 0x3];
        }
    }

    public override string ReadFString()
    {
        var length = Read<int>();
        if (length == 0) return string.Empty;

        var strLength = length > 0 ? length : -length * sizeof(ushort);
        if (strLength > Length - Position) throw new ParserException($"Invalid FString length '{strLength}'");

        Span<byte> strBuffer = strLength <= 1024 ? stackalloc byte[strLength] : new byte[strLength];
        ReadExactly(strBuffer);

        if (length > 0)
        {
            return Encoding.UTF8.GetString(strBuffer[..^1]);
        }
        else
        {
            for (int i = 0; i < strBuffer.Length; i++)
            {
                strBuffer[i] ^= _xorKeyLong[i & 0x7];
            }
            return Encoding.Unicode.GetString(strBuffer[..^2]);
        }
    }

    public override FName ReadFName() => ReadFString();
}

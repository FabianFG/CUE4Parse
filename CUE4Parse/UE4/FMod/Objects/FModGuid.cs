using System;
using System.Buffers.Binary;
using System.IO;
using CUE4Parse.UE4.Objects.Core.Misc;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.FMod.Objects;

[JsonConverter(typeof(FModGuidConverter))]
public readonly struct FModGuid
{
    public readonly uint Data1;
    public readonly uint Data2;
    public readonly uint Data3;
    public readonly uint Data4;

    public FModGuid(BinaryReader Ar)
    {
        Data1 = Ar.ReadUInt32();
        Data2 = Ar.ReadUInt32();
        Data3 = Ar.ReadUInt32();
        Data4 = Ar.ReadUInt32();
    }

    public FModGuid(Guid guid)
    {
        var bytes = guid.ToByteArray();
        Data1 = BitConverter.ToUInt32(bytes, 0);
        Data2 = BitConverter.ToUInt32(bytes, 4);
        Data3 = BitConverter.ToUInt32(bytes, 8);
        Data4 = BitConverter.ToUInt32(bytes, 12);
    }

    public FModGuid(FGuid fguid)
    {
        Data1 = fguid.A;
        Data2 = (fguid.B << 16) | (fguid.B >> 16);
        Data3 = BinaryPrimitives.ReverseEndianness(fguid.C);
        Data4 = BinaryPrimitives.ReverseEndianness(fguid.D);
    }

    public FModGuid(string text)
    {
        if (!TryParseInternal(text, out var g))
            throw new FormatException($"Invalid FMOD GUID string: '{text}'");

        this = new FModGuid(g);
    }

    public bool IsEmpty => Data1 == 0 && Data2 == 0 && Data3 == 0 && Data4 == 0;

    public bool Equals(FModGuid other) =>
        Data1 == other.Data1 &&
        Data2 == other.Data2 &&
        Data3 == other.Data3 &&
        Data4 == other.Data4;

    public override bool Equals(object? obj) => obj is FModGuid g && Equals(g);
    public override int GetHashCode() => HashCode.Combine(Data1, Data2, Data3, Data4);

    public Guid ToGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes[..4], Data1);
        BitConverter.TryWriteBytes(bytes.Slice(4, 4), Data2);
        BitConverter.TryWriteBytes(bytes.Slice(8, 4), Data3);
        BitConverter.TryWriteBytes(bytes.Slice(12, 4), Data4);
        return new Guid(bytes);
    }

    public override string ToString() => ToGuid().ToString();

    public static bool operator ==(FModGuid left, FModGuid right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FModGuid left, FModGuid right)
    {
        return !(left == right);
    }

    private static bool TryParseInternal(string? text, out Guid guid)
    {
        guid = Guid.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();

        if (text.Length >= 2 && text[0] == '{' && text[^1] == '}')
            text = text[1..^1];

        if (Guid.TryParse(text, out guid))
            return true;

        if (text.Length == 32)
        {
            try
            {
                string dashed =
                    text[..8] + "-" +
                    text[8..12] + "-" +
                    text[12..16] + "-" +
                    text[16..20] + "-" +
                    text[20..32];

                if (Guid.TryParse(dashed, out guid))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}

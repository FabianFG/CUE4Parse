using System;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FModGuid
{
    public readonly uint Data1;
    public readonly ushort Data2;
    public readonly ushort Data3;
    public readonly byte[] Data4;

    public FModGuid(BinaryReader Ar)
    {
        Data1 = Ar.ReadUInt32();
        Data2 = Ar.ReadUInt16();
        Data3 = Ar.ReadUInt16();
        Data4 = Ar.ReadBytes(8);
    }

    public FModGuid(Guid guid)
    {
        var bytes = guid.ToByteArray();
        Data1 = BitConverter.ToUInt32(bytes, 0);
        Data2 = BitConverter.ToUInt16(bytes, 4);
        Data3 = BitConverter.ToUInt16(bytes, 6);
        Data4 = new byte[8];
        Buffer.BlockCopy(bytes, 8, Data4, 0, 8);
    }

    public FModGuid(string text)
    {
        if (!TryParseInternal(text, out var g))
            throw new FormatException($"Invalid FMOD GUID string: '{text}'");

        this = new FModGuid(g);
    }

    public FModGuid(FGuid fguid)
    {
        Data1 = fguid.A;
        Data2 = (ushort) ((fguid.B >> 16) & 0xFFFF);
        Data3 = (ushort) (fguid.B & 0xFFFF);
        Data4 =
        [
                (byte)(fguid.C >> 24),
                (byte)(fguid.C >> 16),
                (byte)(fguid.C >> 8),
                (byte)(fguid.C),
                (byte)(fguid.D >> 24),
                (byte)(fguid.D >> 16),
                (byte)(fguid.D >> 8),
                (byte)(fguid.D)
        ];
    }

    public bool IsEmpty =>
        Data1 == 0 &&
        Data2 == 0 &&
        Data3 == 0 &&
        (Data4 == null || Data4.All(b => b == 0));

    public bool Equals(FModGuid other)
    {
        return Data1 == other.Data1 &&
               Data2 == other.Data2 &&
               Data3 == other.Data3 &&
               Data4.AsSpan().SequenceEqual(other.Data4);
    }

    public override bool Equals(object? obj) => obj is FModGuid g && Equals(g);
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Data1);
        hash.Add(Data2);
        hash.Add(Data3);
        foreach (var b in Data4)
            hash.Add(b);
        return hash.ToHashCode();
    }

    public override readonly string ToString()
    {
        return $"{Data1:x8}-{Data2:x4}-{Data3:x4}-{BitConverter.ToString(Data4, 0, 2).Replace("-", "")}-{BitConverter.ToString(Data4, 2, 6).Replace("-", "")}";
    }

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

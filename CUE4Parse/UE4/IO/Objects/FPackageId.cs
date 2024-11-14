using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static CUE4Parse.Utils.CityHash;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPackageId : IEquatable<FPackageId>
{
    public readonly ulong id;

    public FPackageId(ulong id)
    {
        this.id = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FPackageId other) => id == other.id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is FPackageId other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => id.GetHashCode();

    public override string ToString() => id.ToString();

    public static FPackageId FromName(string name)
    {
        Span<char> result = name.Length < 256 ? stackalloc char[name.Length] : new char[name.Length].AsSpan();
        ToLower(name, result);
        var nameBuf = Encoding.Unicode.GetBytes(result.ToArray());
        var hash = CityHash64(nameBuf);
        Trace.Assert(hash != ~0uL, $"Package name hash collision \"{name}\" and InvalidId");
        return new FPackageId(hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ToLower(char input)
    {
        return (char) ((uint) input + ((((uint) input - 'A' < 26u) ? 1 : 0) << 5));
    }

    public static void ToLower(string input, Span<char> result)
    {
        for (var i = 0; i < input.Length; i++)
        {
            result[i] = ToLower(input[i]);
        }
    }
}

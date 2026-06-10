using System.Runtime.CompilerServices;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Core.Misc;

[JsonConverter(typeof(FSHAHashJsonConverter))]
public readonly struct FSHAHash : IUStruct, IEquatable<FSHAHash>
{
    public const int SIZE = 20;
    public readonly Bytes20 Hash;

    public FSHAHash(FArchive Ar)
    {
        Hash = default;
        Ar.ReadExactly(Hash);
    }

    public FSHAHash(FArchive Ar, int customSize)
    {
        Hash = default;
        if (customSize <= SIZE)
            Ar.ReadExactly(Hash[..customSize]);
        else
        {
            Ar.ReadExactly(Hash);
            Ar.Position += customSize - SIZE;
        }
    }

    public FSHAHash(FIoChunkHash chunkHash)
    {
        Hash = default;
        ReadOnlySpan<byte> src = chunkHash.Hash[..SIZE];
        Span<byte> dst = Hash;

        src.CopyTo(dst);
    }

    public static implicit operator FSHAHash(FIoChunkHash InChunkHash) => new(InChunkHash);

    public override string ToString()
    {
        ReadOnlySpan<byte> span = Hash;
        return Convert.ToHexString(span);
    }

    public bool IsValid()
    {
        ReadOnlySpan<byte> span = Hash;
        return span.IndexOfAnyExcept((byte) 0) < 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FSHAHash other)
    {
        ReadOnlySpan<byte> left = Hash;
        ReadOnlySpan<byte> right = other.Hash;

        return left.SequenceEqual(right);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is FSHAHash other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FSHAHash left, FSHAHash right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FSHAHash left, FSHAHash right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        ReadOnlySpan<byte> span = Hash;

        return HashCode.Combine(
            BitConverter.ToUInt64(span[..8]),
            BitConverter.ToUInt64(span[8..16]),
            BitConverter.ToUInt32(span[16..20])
        );
    }

    [InlineArray(SIZE)]
    public struct Bytes20
    {
        public byte Hash;
    }
}

public sealed class FSHAHashJsonConverter : JsonConverter<FSHAHash>
{
    public override bool CanRead => false;

    public override FSHAHash ReadJson(JsonReader reader, Type objectType, FSHAHash existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FSHAHash value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}

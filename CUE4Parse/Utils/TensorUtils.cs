using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.Utils;

public static class TensorUtils
{
    private static void XorScalar<T>(Span<byte> data, ReadOnlySpan<byte> key) where T : unmanaged, IBitwiseOperators<T, T, T>
    {
        int size = Unsafe.SizeOf<T>();
        int remainder = BitOperations.IsPow2((uint) size) ? data.Length & (size - 1) : data.Length % size;
        int end = data.Length - remainder;

        var span = MemoryMarshal.Cast<byte, T>(data[..end]);
        var value = MemoryMarshal.Read<T>(key);

        TensorPrimitives.Xor(span, value, span);

        if (remainder != 0)
        {
            var tail = data[end..];
            TensorPrimitives.Xor(tail, key[..remainder], tail);
        }
    }

    public static void Xor<T>(Span<T> data, T xorKey) where T : unmanaged, IBitwiseOperators<T, T, T>
    {
        TensorPrimitives.Xor(data, xorKey, data);
    }

    public static void Xor(Span<byte> data, ReadOnlySpan<byte> xorKey)
    {
        int dataLength = data.Length;
        int keyLength = xorKey.Length;
        if (dataLength == 0 || keyLength == 0)
            return;

        if (dataLength <= keyLength)
        {
            TensorPrimitives.Xor(data, xorKey[..dataLength], data);
            return;
        }

        switch (keyLength)
        {
            case 1:
                TensorPrimitives.Xor(data, xorKey[0], data);
                return;
            case 2:
                XorScalar<ushort>(data, xorKey);
                return;
            case 4:
                XorScalar<uint>(data, xorKey);
                return;
            case 8:
                XorScalar<ulong>(data, xorKey);
                return;
            case 12:
                XorScalar<Bytes12>(data, xorKey);
                return;
            case 16:
                XorScalar<UInt128>(data, xorKey);
                return;
            case 20:
                XorScalar<Bytes20>(data, xorKey);
                return;
            case 24:
                XorScalar<Bytes24>(data, xorKey);
                return;
            case 28:
                XorScalar<Bytes28>(data, xorKey);
                return;
            case 32:
                XorScalar<Bytes32>(data, xorKey);
                return;
            case 36:
                XorScalar<Bytes36>(data, xorKey);
                return;
            default:
                var remainder = dataLength % keyLength;
                int end = dataLength - remainder;
                for (int offset = 0; offset < end; offset += keyLength)
                {
                    var span = data.Slice(offset, keyLength);
                    TensorPrimitives.Xor(span, xorKey, span);
                }

                if (remainder > 0)
                {
                    var span = data[^remainder..];
                    TensorPrimitives.Xor(span, xorKey[..remainder], span);
                }
                return;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public record struct Bytes12(ulong Part1, uint Part2) : IBitwiseOperators<Bytes12, Bytes12, Bytes12>
    {
        public static Bytes12 operator ~(Bytes12 value) => new(~value.Part1, ~value.Part2);
        public static Bytes12 operator &(Bytes12 left, Bytes12 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2);
        public static Bytes12 operator |(Bytes12 left, Bytes12 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2);
        public static Bytes12 operator ^(Bytes12 left, Bytes12 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2);
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    public record struct Bytes20(UInt128 Part1, uint Part2) : IBitwiseOperators<Bytes20, Bytes20, Bytes20>
    {
        public static Bytes20 operator ~(Bytes20 value) => new(~value.Part1, ~value.Part2);
        public static Bytes20 operator &(Bytes20 left, Bytes20 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2);
        public static Bytes20 operator |(Bytes20 left, Bytes20 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2);
        public static Bytes20 operator ^(Bytes20 left, Bytes20 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2);
    }

    [StructLayout(LayoutKind.Sequential, Size = 24)]
    public record struct Bytes24(ulong Part1, ulong Part2, ulong Part3) : IBitwiseOperators<Bytes24, Bytes24, Bytes24>
    {
        public static Bytes24 operator ~(Bytes24 value) => new(~value.Part1, ~value.Part2, ~value.Part3);
        public static Bytes24 operator &(Bytes24 left, Bytes24 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2, left.Part3 & right.Part3);
        public static Bytes24 operator |(Bytes24 left, Bytes24 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2, left.Part3 | right.Part3);
        public static Bytes24 operator ^(Bytes24 left, Bytes24 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2, left.Part3 ^ right.Part3);
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    public record struct Bytes28(UInt128 Part1, Bytes12 Part2) : IBitwiseOperators<Bytes28, Bytes28, Bytes28>
    {
        public static Bytes28 operator ~(Bytes28 value) => new(~value.Part1, ~value.Part2);
        public static Bytes28 operator &(Bytes28 left, Bytes28 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2);
        public static Bytes28 operator |(Bytes28 left, Bytes28 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2);
        public static Bytes28 operator ^(Bytes28 left, Bytes28 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2);
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public record struct Bytes32(UInt128 Part1, UInt128 Part2) : IBitwiseOperators<Bytes32, Bytes32, Bytes32>
    {
        public static Bytes32 operator ~(Bytes32 value) => new(~value.Part1, ~value.Part2);
        public static Bytes32 operator &(Bytes32 left, Bytes32 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2);
        public static Bytes32 operator |(Bytes32 left, Bytes32 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2);
        public static Bytes32 operator ^(Bytes32 left, Bytes32 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2);
    }

    [StructLayout(LayoutKind.Sequential, Size = 36)]
    public record struct Bytes36(UInt128 Part1, UInt128 Part2, uint Part3) : IBitwiseOperators<Bytes36, Bytes36, Bytes36>
    {
        public static Bytes36 operator ~(Bytes36 value) => new(~value.Part1, ~value.Part2, ~value.Part3);
        public static Bytes36 operator &(Bytes36 left, Bytes36 right) => new(left.Part1 & right.Part1, left.Part2 & right.Part2, left.Part3 & right.Part3);
        public static Bytes36 operator |(Bytes36 left, Bytes36 right) => new(left.Part1 | right.Part1, left.Part2 | right.Part2, left.Part3 | right.Part3);
        public static Bytes36 operator ^(Bytes36 left, Bytes36 right) => new(left.Part1 ^ right.Part1, left.Part2 ^ right.Part2, left.Part3 ^ right.Part3);
    }
}

using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class NaniteUtils
{
    /// <summary>The maximum number of bits used to serialize normals.</summary>
    public static int NANITE_MAX_NORMAL_QUANTIZATION_BITS(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_2 => NANITE_MAX_NORMAL_QUANTIZATION_BITS_502,
            _ => NANITE_MAX_NORMAL_QUANTIZATION_BITS_500
        };
    }
    /// <summary>The maximum number of bits used to serialize normals.</summary>
    public static int NANITE_MAX_TEXCOORD_QUANTIZATION_BITS(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_4 => NANITE_MAX_TEXCOORD_QUANTIZATION_BITS_504,
            _ => NANITE_MAX_TEXCOORD_QUANTIZATION_BITS_500
        };
    }

    /// <summary>The minimum multiplier used to compute the position delta of a vertex within a cluster.</summary>
    public static int NANITE_MIN_POSITION_PRECISION(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_4 => NANITE_MIN_POSITION_PRECISION_504,
            _ => NANITE_MIN_POSITION_PRECISION_500
        };
    }
    /// <summary>The maximum multiplier used to compute the position delta of a vertex within a cluster.</summary>
    public static int NANITE_MAX_POSITION_PRECISION(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_4 => NANITE_MAX_POSITION_PRECISION_504,
            _ => NANITE_MAX_POSITION_PRECISION_500
        };
    }

    public static int NANITE_MAX_CLUSTERS_PER_PAGE_BITS(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_4 => NANITE_MAX_CLUSTERS_PER_PAGE_BITS_504,
            _ => NANITE_MAX_CLUSTERS_PER_PAGE_BITS_500
        };
    }

    public static int NANITE_MAX_CLUSTERS_PER_PAGE(EGame ver)
    {
        return ver switch
        {
            >= EGame.GAME_UE5_4 => 1 << NANITE_MAX_CLUSTERS_PER_PAGE_BITS_504,
            _ => 1 << NANITE_MAX_CLUSTERS_PER_PAGE_BITS_500
        };
    }

    public readonly static ImmutableDictionary<int, float> PrecisionScales;

    static NaniteUtils()
    {
        ImmutableDictionary<int, float>.Builder builder = ImmutableDictionary.CreateBuilder<int, float>();
        for (int i = -32; i <= 32; i++)
        {
            int temp = 0;
            Unsafe.As<int, float>(ref temp) = 1.0f;
            float scale = 1.0f;
            Unsafe.As<float, int>(ref scale) = temp - (i << 23);
            builder.Add(i, scale);
        }
        PrecisionScales = builder.ToImmutable();
    }

    /// <summary>Equivalent to BitFieldExtractU32.</summary>
    public static uint GetBits(uint value, int numBits, int offset)
    {
        uint mask = (1u << numBits) - 1u;
        return (value >> offset) & mask;
    }

    public static int UIntToInt(uint value, int bitLength)
    {
        return unchecked((int) (value << (32-bitLength)) ) >> (32-bitLength);
    }

    /// <summary>Equivalent to BitFieldExtractS32.</summary>
    public static int GetBitsAsSigned(uint value, int numBits, int offset)
    {
        return UIntToInt(GetBits(value, numBits, offset), numBits);
    }

    public static uint BitAlignU32(uint high, uint low, long shift)
    {
        shift = shift & 31u;
        uint result = low >> (int)shift;
        result |= shift > 0 ? (high << (32 - (int) shift)) : 0u;
        return result;
    }

    public static uint BitFieldMaskU32(int maskWidth, int maskLocation)
    {
        maskWidth &= 31;
        maskLocation &= 31;
        return ((1u << maskWidth) - 1) << maskLocation;
    }

    /// <summary>
    /// Reads a non-byte aligned uint from an archive.
    /// </summary>
    /// <param name="Ar">The archive to read from.</param>
    /// <param name="baseAddressInBytes">A byte aligned position use as an anchor.</param>
    /// <param name="bitOffset">The offset in bits from the aligned location.</param>
    /// <returns></returns>
    public static uint ReadUnalignedDword(FArchive Ar, long baseAddressInBytes, long bitOffset)
    {
        long byteAddress = baseAddressInBytes + (bitOffset >> 3);
        long alignedByteAddress = byteAddress & ~3;
        bitOffset = ((byteAddress - alignedByteAddress) << 3) | (bitOffset & 7);
        Ar.Position = alignedByteAddress;
        uint low = Ar.Read<uint>();
        uint high = Ar.Read<uint>();
        return BitAlignU32(high, low, bitOffset);
    }

    public static uint UnpackByte0(uint v) => v & 0xff;
    public static uint UnpackByte1(uint v) => (v >> 8) & 0xff;
    public static uint UnpackByte2(uint v) => (v >> 16) & 0xff;
    public static uint UnpackByte3(uint v) => v >> 24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeZigZag(uint data)
    {
        return (int)(data >> 1) ^ -(int)(data & 1);
        //return (int)(data >> 1) ^ GetBitsAsSigned(data, 1 ,0);
    }

    /// <summary>
    /// Finds the location of the highest populated bit in a uint.
    /// Essentially just the ReverseBitScan instruction with a defined behaviour if the value is 0.
    /// </summary>
    /// <returns>the index of the first bit or uint.MAX_VALUE if not found.</returns>
    public static uint FirstBitHigh(uint x)
    {
        return x == 0 ? 0xFFFFFFFFu : (uint) BitOperations.Log2(x);
    }

    public static FUIntVector LowMidHighIncrement(uint numBytesPerValue, uint num)
    {
        return new FUIntVector(
            numBytesPerValue >= 1 ? num : 0u,
            numBytesPerValue >= 2 ? num : 0u,
            numBytesPerValue >= 3 ? num : 0u
        );
    }

    public class LMHStreamReader
    {
        private readonly FArchive _archive;

        public LMHStreamReader(FArchive archive)
        {
            _archive = archive;
        }

        public Vector128<int> Read(FUIntVector lowMidHighOffsets, uint components, int count, uint index, ref Vector128<int> prevLastValue)
        {
            var position = lowMidHighOffsets + index * count;
            Span<byte> span = stackalloc byte[4];
            var buffer = span[..count];
            Vector128<uint> packed = Vector128<uint>.Zero;

            if (components >= 3)
            {
                _archive.ReadAt((long)position[2], buffer);
                packed |= Vector128.Create<uint>([span[0], span[1], span[2], span[3]]) << 16;
                span.Clear();
            }

            if (components >= 2)
            {
                _archive.ReadAt((long)position[1], buffer);
                packed |= Vector128.Create<uint>([span[0], span[1], span[2], span[3]]) << 8;
                span.Clear();
            }

            if (components >= 1)
            {
                _archive.ReadAt((long)position[0], buffer);
                packed |= Vector128.Create<uint>([span[0], span[1], span[2], span[3]]);
            }

            Span<int> decoded = stackalloc int[4];
            for (int i = 0; i < count; i++)
                decoded[i] = DecodeZigZag(packed[i]);

            var value = Vector128.Create<int>(decoded) + prevLastValue;
            prevLastValue = value;

            return value;
        }
    }

    public static BitStreamReader CreateBitStreamReader_Aligned(long byteAddress, long bitOffset, long compileTimeMaxRemainingBits)
    {
        return new BitStreamReader(byteAddress, bitOffset, compileTimeMaxRemainingBits);
    }

    public static BitStreamReader CreateBitStreamReader(long byteAddress, long bitOffset, long compileTimeMaxRemainingBits)
    {
        long AlignedByteAddress = byteAddress & ~3;
        bitOffset += (byteAddress & 3) << 3;
        return new BitStreamReader(AlignedByteAddress, bitOffset, compileTimeMaxRemainingBits);
    }

    public class BitStreamReader
    {
        long AlignedByteAddress;
        long BitOffsetFromAddress;
        long CompileTimeMaxRemainingBits;

        uint[] BufferBits = [0, 0, 0, 0];
        long BufferOffset = 0;
        long CompileTimeMinBufferBits = 0;
        long CompileTimeMinDwordBits = 0;

        public BitStreamReader(long alignedByteAddress, long bitOffset, long compileTimeMaxRemainingBits)
        {
            AlignedByteAddress = alignedByteAddress;
            BitOffsetFromAddress = bitOffset;
            CompileTimeMaxRemainingBits = compileTimeMaxRemainingBits;
        }

        public uint Read(FArchive Ar, int numBits, int compileTimeMaxBits)
        {
            if (compileTimeMaxBits > CompileTimeMinBufferBits)
            {
                // BitBuffer could be out of bits: Reload.

                // Add cumulated offset since last refill. No need to update at every read.
                BitOffsetFromAddress += BufferOffset;
                long address = AlignedByteAddress + ((BitOffsetFromAddress >> 5) << 2);

                // You have to be a bit weird about it because it tries
                // to read from out of bounds, which is not great NGL
                Ar.Position = address;
                uint[] data = [0, 0, 0, 0];
                for (int i = 0; i < data.Length; i++)
                {
                    if (Ar.Position + sizeof(uint) <= Ar.Length)
                    {
                        data[i] = Ar.Read<uint>();
                    }
                    else if (Ar.Position == Ar.Length)
                    {
                        // safety
                        data[i] = 0;
                    }
                    else
                    {
                        uint value = 0u;
                        byte[] bytes = Ar.ReadBytes((int) Math.Min(sizeof(uint), Ar.Position - Ar.Length));
                        for (int j = 0; j < bytes.Length; j++)
                        {
                            value |= (uint) bytes[j] << (j * 8);
                        }
                        data[i] = value;
                    }
                }
                // Shift bits down to align
                BufferBits[0] = BitAlignU32(data[1], data[0], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 32) BufferBits[1] = BitAlignU32(data[2], data[1], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 64) BufferBits[2] = BitAlignU32(data[3], data[2], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 96) BufferBits[3] = BitAlignU32(0, data[3], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31

                BufferOffset = 0;
                CompileTimeMinDwordBits = Math.Min(32, CompileTimeMaxRemainingBits);
                CompileTimeMinBufferBits = Math.Min(97, CompileTimeMaxRemainingBits); // Up to 31 bits wasted to alignment

            }
            else if (compileTimeMaxBits > CompileTimeMinDwordBits)
            {
                // Bottom dword could be out of bits: Shift down.
                BitOffsetFromAddress += BufferOffset;

                // Workaround for BitAlignU32(x, y, 32) returning x instead of y.
                // In the common case where State.CompileTimeMinDwordBits != 0, this will be optimized to just BitAlignU32.
                // sTODO: Can we get rid of this special case?
                bool offset32 = CompileTimeMinDwordBits == 0 && BufferOffset == 32;

                BufferBits[0]                                    = offset32 ? BufferBits[1] : BitAlignU32(BufferBits[1], BufferBits[0], BufferOffset);
                if (CompileTimeMinBufferBits > 32) BufferBits[1] = offset32 ? BufferBits[2] : BitAlignU32(BufferBits[2], BufferBits[1], BufferOffset);
                if (CompileTimeMinBufferBits > 64) BufferBits[2] = offset32 ? BufferBits[3] : BitAlignU32(BufferBits[3], BufferBits[2], BufferOffset);
                if (CompileTimeMinBufferBits > 96) BufferBits[3] = offset32 ? 0             : BitAlignU32(0,             BufferBits[3], BufferOffset);

                BufferOffset = 0;

                CompileTimeMinDwordBits = Math.Min(32, CompileTimeMaxRemainingBits);
            }

            uint result = GetBits(BufferBits[0], numBits, (int)BufferOffset); // BufferOffset implicitly &31
            BufferOffset += numBits;
            CompileTimeMinBufferBits -= compileTimeMaxBits;
            CompileTimeMinDwordBits -= compileTimeMaxBits;
            CompileTimeMaxRemainingBits -= compileTimeMaxBits;

            return result;
        }
    }
}

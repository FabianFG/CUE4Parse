using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.Core.Compression;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FCompressedBufferHeader
{
    public const uint ExpectedMagic = 0xb7756362;

    /** A magic number to identify a compressed buffer. Always 0xb7756362. */
    public uint Magic = ExpectedMagic;
    /** A CRC-32 used to check integrity of the buffer. Uses the polynomial 0x04c11db7. */
    public uint Crc32 = 0;
    /** The method used to compress the buffer. Affects layout of data following the header. */
    [JsonConverter(typeof(StringEnumConverter))]
    public EMethod Method = EMethod.None;
    /** The method-specific compressor used to compress the buffer. */
    public byte Compressor = 0;
    /** The method-specific compression level used to compress the buffer. */
    public byte CompressionLevel = 0;
    /** The power of two size of every uncompressed block except the last. Size is 1 << BlockSizeExponent. */
    public byte BlockSizeExponent = 0;
    /** The number of blocks that follow the header. */
    public uint BlockCount = 0;
    /** The total size of the uncompressed data. */
    public ulong TotalRawSize = 0;
    /** The total size of the compressed data including the header. */
    public ulong TotalCompressedSize = 0;
    /** The hash of the uncompressed data. */
    public byte[] RawHash;

    public FCompressedBufferHeader() { }

    public FCompressedBufferHeader(FArchive Ar)
    {
        Magic = BinaryPrimitives.ReverseEndianness(Ar.Read<uint>());
        if (Magic != ExpectedMagic)
        {
            throw new Exception($"FCompressedBuffer has invalid magic number: 0x{Magic:X8}");
        }

        Crc32 = BinaryPrimitives.ReverseEndianness(Ar.Read<uint>());
        Method = Ar.Read<EMethod>();
        Compressor = Ar.Read<byte>();
        CompressionLevel = Ar.Read<byte>();
        BlockSizeExponent = Ar.Read<byte>();
        BlockCount = BinaryPrimitives.ReverseEndianness(Ar.Read<uint>());
        TotalRawSize = BinaryPrimitives.ReverseEndianness(Ar.Read<ulong>());
        TotalCompressedSize = BinaryPrimitives.ReverseEndianness(Ar.Read<ulong>());
        RawHash = Ar.ReadArray<byte>(32);
    }

    public enum EMethod : byte
    {
        /** Header is followed by one uncompressed block. */
        None = 0,
        /** Header is followed by an array of compressed block sizes then the compressed blocks. */
        Oodle = 3,
        /** Header is followed by an array of compressed block sizes then the compressed blocks. */
        LZ4 = 4,
    }
};

using System;
using System.Buffers.Binary;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

public enum EMethod : byte
{
    /** Header is followed by one uncompressed block. */
    None = 0,
    /** Header is followed by an array of compressed block sizes then the compressed blocks. */
    Oodle = 3,
    /** Header is followed by an array of compressed block sizes then the compressed blocks. */
    LZ4 = 4,
}

[Flags]
public enum EFlags
{
    /** No flags are set */
    None						= 0,
    /** Is the data actually virtualized or not? */
    IsVirtualized				= 1 << 0,
    /** Does the package have access to a .upayload file? */
    HasPayloadSidecarFile		= 1 << 1,
    /** The bulkdata object is currently referencing a payload saved under old bulkdata formats */
    ReferencesLegacyFile		= 1 << 2,
    /** The legacy file being referenced is stored with Zlib compression format */
    LegacyFileIsCompressed		= 1 << 3,
    /** The payload should not have compression applied to it. It is assumed that the payload is already
        in some sort of compressed format, see the compression documentation above for more details. */
    DisablePayloadCompression	= 1 << 4,
    /** The legacy file being referenced derived its key from guid and it should be replaced with a key-from-hash when saved */
    LegacyKeyWasGuidDerived		= 1 << 5,
    /** (Transient) The Guid has been registered with the BulkDataRegistry */
    HasRegistered				= 1 << 6,
    /** (Transient) The BulkData object is a copy used only to represent the id and payload; it does not communicate with the BulkDataRegistry, and will point DDC jobs toward the original BulkData */
    IsTornOff					= 1 << 7,
    /** The bulkdata object references a payload stored in a WorkspaceDomain file  */
    ReferencesWorkspaceDomain	= 1 << 8,
    /** The payload is stored in a package trailer, so the bulkdata object will have to poll the trailer to find the payload offset */
    StoredInPackageTrailer		= 1 << 9,
    /** The bulkdata object was cooked. */
    IsCooked					= 1 << 10,
    /** (Transient) The package owning the bulkdata has been detached from disk and we can no longer load from it */
    WasDetached					= 1 << 11
}

public class FHeader
{
    /** A magic number to identify a compressed buffer. Always 0xb7756362. */
    public uint Magic { get; set; }
    
    /** A CRC-32 used to check integrity of the buffer. Uses the polynomial 0x04c11db7. */
    public int Crc32 { get; set; }
    
    /** The method used to compress the buffer. Affects layout of data following the header. */
    public EMethod Method { get; set; }
    
    /** The method-specific compressor used to compress the buffer. */
    public byte Compressor { get; set; }
    
    /** The method-specific compression level used to compress the buffer. */
    public byte CompressionLevel { get; set; }
    
    /** The power of two size of every uncompressed block except the last. Size is 1 << BlockSizeExponent. */
    public byte BlockSizeExponent { get; set; }
    
    /** The number of blocks that follow the header. */
    public int BlockCount { get; set; }
    
    /** The total size of the uncompressed data. */
    public long TotalRawSize { get; set; }
    
    /** The total size of the compressed data including the header. */
    public long TotalCompressedSize { get; set; }
    
    public FHeader(FAssetArchive Ar)
    {
        var ExpectedMagic = 0xb7756362; // <dot>ucb
        Magic = Ar.Read<uint>();
        Magic = BinaryPrimitives.ReverseEndianness(Magic);
        if (Magic != ExpectedMagic)
        {
            throw new ParserException("EditorBulkData: bad magic number");
        }
        
        Crc32 = Ar.Read<int>();
        Crc32 = BinaryPrimitives.ReverseEndianness(Crc32);
        Method = Ar.Read<EMethod>();
        if (Method != EMethod.None)
        {
            throw new NotSupportedException("EditorBulkData: Compression not supported");
        }
        
        Compressor = Ar.Read<byte>();
        CompressionLevel = Ar.Read<byte>();
        BlockSizeExponent = Ar.Read<byte>();
        BlockCount = Ar.Read<int>();
        BlockCount = BinaryPrimitives.ReverseEndianness(BlockCount);

        TotalRawSize = Ar.Read<long>();
        TotalRawSize = BinaryPrimitives.ReverseEndianness(TotalRawSize);

        TotalCompressedSize = Ar.Read<long>();
        TotalCompressedSize = BinaryPrimitives.ReverseEndianness(TotalCompressedSize);
        Ar.Position += 32; // FBlake3Hash
    }
}

public class FEditorBulkData
{
    public FHeader Header { get; private set; }
    public EFlags Flags { get; set; }
    public FGuid BulkDataId { get; set; }
    public FSHAHash PayloadContentId { get; set; }
    public long PayloadSize { get; set; }
    public int Offset { get; set; }
    public byte[] RawData { get; set; }
    
    public FEditorBulkData(FAssetArchive Ar)
    {
        Flags = Ar.Read<EFlags>();
        BulkDataId = Ar.Read<FGuid>();
        PayloadContentId = new FSHAHash(Ar);
        PayloadSize = Ar.Read<long>();
        if (Flags.HasFlag(EFlags.StoredInPackageTrailer))
        {
            // Seems to be something like bulkdata payloads? not sure
            Log.Error("EditorBulkData: Stored In Package Trailer is not supported.");
            return;
        }
        Offset = Ar.Read<int>();

        Ar.Position = Offset;

        Header = new FHeader(Ar);
        RawData = Ar.ReadBytes((int)Header.TotalRawSize);
        Ar.Read<int>(); // FPackage Magic
    }
}
using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Compression;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets;

public class FPackageTrailer
{
    private readonly FAssetArchive Package;
    public FHeader Header;
    public FFooter Footer;

    public FPackageTrailer(FAssetArchive Ar)
    {
        Package = (FAssetArchive)Ar.Clone();
        Header = new FHeader(Ar);
        Ar.Position += (long)Header.PayloadsDataLength;
        Footer = new FFooter(Ar);
    }

    public long FindPayloadOffsetInFile(FSHAHash id)
    {
        if (id.Hash.All(b => b == 0) || Header.PayloadLookupTable is null)
            return -1;

        foreach (var entry in Header.PayloadLookupTable)
        {
            if (entry.Identifier.Hash.SequenceEqual(id.Hash))
            {
                return entry.AccessMode switch
                {
                    EPayloadAccessMode.Local => Header.TrailerPositionInFile + Header.HeaderLength + entry.OffsetInFile,
                    EPayloadAccessMode.Referenced => entry.OffsetInFile,
                    _ => -1
                };
            }
        }

        return -1;
    }

    FCompressedBuffer LoadPayloadById(FSHAHash Id)
    {
        var OffsetInFile = FindPayloadOffsetInFile(Id);
        if (OffsetInFile == -1)
            return new FCompressedBuffer();

        Package.Position = OffsetInFile;
        return new FCompressedBuffer(Package);
    }

    public readonly struct FHeader
    {
        /** Unique value used to identify the header */
        const ulong HeaderTag = 0xD1C43B2E80A5F697;

        /** Expected tag at the start of the header */
        public readonly ulong Tag = 0;
        /** Version of the header */
        public readonly int Version = -1;
        /** Total length of the header on disk in bytes */
        public readonly uint HeaderLength = 0;
        /** Total length of the payloads on disk in bytes */
        public readonly ulong PayloadsDataLength = 0;
        /** Lookup table for the payloads on disk */
        public readonly FLookupTableEntry[] PayloadLookupTable;

        public readonly long TrailerPositionInFile;

        public FHeader(FArchive Ar)
        {
            TrailerPositionInFile = Ar.Position;
            Tag = Ar.Read<ulong>();
            if (Tag != HeaderTag)
            {
                throw new ParserException(Ar, $"Invalid package trailer header tag: {Tag:X16}");
            }

            Version = Ar.Read<int>();
            HeaderLength = Ar.Read<uint>();
            PayloadsDataLength = Ar.Read<ulong>();
            if (TrailerPositionInFile + HeaderLength + (long) PayloadsDataLength > Ar.Length)
            {
                throw new ParserException(Ar, $"Invalid package trailer header length: {HeaderLength} + {PayloadsDataLength} exceeds file size");
            }

            var LegacyAccessMode = EPayloadAccessMode.Local;
            if (Version < (uint) EPackageTrailerVersion.ACCESS_PER_PAYLOAD)
            {
                LegacyAccessMode = Ar.Read<EPayloadAccessMode>();
            }

            var version = Version;
            PayloadLookupTable = Ar.ReadArray(() => new FLookupTableEntry(Ar, version));
            for (int i = 0; i < PayloadLookupTable.Length; i++)
            {
                var entry = PayloadLookupTable[i];
                if (Version < (uint) EPackageTrailerVersion.ACCESS_PER_PAYLOAD)
                {
                    entry.AccessMode = entry.OffsetInFile != -1 ? LegacyAccessMode : EPayloadAccessMode.Virtualized;
                }
            }
        }
    }

    public readonly struct FFooter
    {
        /** Unique value used to identify the footer */
        const ulong FooterTag = 0x29BFCA045138DE76;

        /** Expected tag at the start of the footer */
        public readonly ulong Tag = 0;
        /** Total length of the trailer on disk in bytes */
        public readonly ulong TrailerLength = 0;
        /** End the trailer with PACKAGE_FILE_TAG, which we expect all package files to end with */
        public readonly uint PackageTag = 0;

        public FFooter(FArchive Ar)
        {
            Tag = Ar.Read<ulong>();
            if (Tag != FooterTag)
            {
                throw new ParserException(Ar, $"Invalid package trailer footer tag: {Tag:X16}");
            }
            TrailerLength = Ar.Read<ulong>();
            PackageTag = Ar.Read<uint>();
        }
    }

    public struct FLookupTableEntry
    {
        /** Identifier for the payload */
        public FSHAHash Identifier; // FIoHash
        /** The offset into the file where we can find the payload, note that a virtualized payload will have an offset of INDEX_NONE */
        public long OffsetInFile = -1;
        /** The size of the payload when compressed. This will be the same value as RawSize if the payload is not compressed */
        public ulong CompressedSize = ulong.MaxValue;
        /** The size of the payload when uncompressed. */
        public ulong RawSize = ulong.MaxValue;
        /** Bitfield of flags, see @UE::EPayloadFlags */
        public EPayloadFlags Flags = EPayloadFlags.None;
        /** Bitfield of flags showing if the payload allowed to be virtualized or the reason why it cannot be virtualized, see @UE::EPayloadFilterReason */
        public EPayloadFilterReason FilterFlags = EPayloadFilterReason.None;
        public EPayloadAccessMode AccessMode = EPayloadAccessMode.Local;

        public FLookupTableEntry(FArchive Ar, int Version)
        {
            Identifier = new FSHAHash(Ar);
            OffsetInFile = Ar.Read<long>();
            CompressedSize = Ar.Read<ulong>();
            RawSize = Ar.Read<ulong>();

            if (Version >= (uint) EPackageTrailerVersion.PAYLOAD_FLAGS)
            {
                Flags = Ar.Read<EPayloadFlags>();
                FilterFlags = Ar.Read<EPayloadFilterReason>();
            }
            
            if (Version >= (uint)EPackageTrailerVersion.ACCESS_PER_PAYLOAD)
            {
                AccessMode = Ar.Read<EPayloadAccessMode>();
            }
        }
    }

    public enum EPackageTrailerVersion : uint
    {
        // The original trailer format when it was first added
        INITIAL = 0,
        // Access mode is now per payload and found in FLookupTableEntry 
        ACCESS_PER_PAYLOAD = 1,
        // Added EPayloadAccessMode to FLookupTableEntry
        PAYLOAD_FLAGS = 2,

        // -----<new versions can be added before this line>-------------------------------------------------
        // - this needs to be the last line (see note below)
        AUTOMATIC_VERSION_PLUS_ONE,
        AUTOMATIC_VERSION = AUTOMATIC_VERSION_PLUS_ONE - 1
    }

    /** Lists the various methods of payload access that the trailer supports */
    public enum EPayloadAccessMode : byte
    {
        /** The payload is stored in the Payload Data segment of the trailer and the offsets in FLookupTableEntry will be relative to the start of this segment */
        Local = 0,
        /** The payload is stored in another package trailer (most likely the workspace domain package file) and the offsets in FLookupTableEntry are absolute offsets in that external file */
        Referenced,
        /** The payload is virtualized and needs to be accessed via IVirtualizationSystem */
        Virtualized
    }

    /** Flags that can be set on payloads in a payload trailer */
    public enum EPayloadFlags : ushort
    {
        /** No flags are set */
        None = 0,
    }

    /** Used to filter requests based on how a payload is stored*/
    public enum EPayloadStorageType : byte
    {
        /** All payload regardless of type. */
        Any,
        /** All payloads stored locally in the package trailer. */
        Local,
        /** All payloads that are a reference to payloads stored in the workspace domain trailer*/
        Referenced,
        /** All payloads stored in a virtualized backend. */
        Virtualized
    }

    /** This enum describes the reasons why a payload may not be virtualized */
    [Flags]
    public enum EPayloadFilterReason : ushort
    {
        /** Not filtered, the payload can be virtualized */
        None = 0,
        /** Filtered due to the asset type of the owning UObject */
        Asset = 1 << 0,
        /** Filtered due to the path of the owning UPackage */
        Path = 1 << 1,
        /** Filtered because the payload size is below the minimum size for virtualization */
        MinSize = 1 << 2,
        /** Filtered because the owning editor bulkdata had virtualization disabled programmatically */
        EditorBulkDataCode = 1 << 3,
        /** Filtered because the package is either a UMap or the owning editor bulkdata is under a UMapBuildDataRegistry */
        MapContent = 1 << 4,
    }
}

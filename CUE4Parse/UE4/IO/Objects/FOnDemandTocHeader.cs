using System;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandTocHeader
{
    public const ulong LegacyExpectedMagic = 0x6f6e64656d616e64; // "ondemand"
    public static readonly byte[] NewExpectedMagic = "UE ON-DEMAND TOC"u8.ToArray();

    public readonly bool IsLegacy;

    public readonly EOnDemandTocVersion LegacyVersion;
    public readonly EOnDemandTocFlags Flags;
    public readonly uint BlockSize;

    public readonly FOnDemandTocVersion Version;
    public readonly long EpochTimestamp;
    public readonly FOnDemandStringEntry BuildVersionEntry;
    public readonly FOnDemandStringEntry TargetPlatformEntry;
    public readonly FOnDemandStringEntry ChunksDirectoryEntry;
    public readonly FOnDemandStringEntry HostGroupNameEntry;
    public readonly FOnDemandStringEntry CompressionFormatEntry;
    public readonly uint StringTableLen;
    public readonly uint ContainerCount;
    public readonly string BuildVersion;
    public readonly string TargetPlatform;

    public readonly string ChunksDirectory;
    public readonly string HostGroupName;
    public readonly string CompressionFormat;

    private readonly byte[] StringTable;
    public string GetString(FOnDemandStringEntry entry) => Encoding.UTF8.GetString(StringTable, (int)entry.Offset, (int)entry.Len);

    public FOnDemandTocHeader(FArchive Ar)
    {
        IsLegacy = Ar.Read<ulong>() == LegacyExpectedMagic;

        if (IsLegacy)
        {
            LegacyVersion = Ar.Read<EOnDemandTocVersion>();
            if (LegacyVersion == EOnDemandTocVersion.Invalid)
                throw new ParserException(Ar, "Invalid iochunktoc version");

            Flags = Ar.Read<EOnDemandTocFlags>();
            BlockSize = Ar.Read<uint>();
            CompressionFormat = Ar.ReadFString();
            ChunksDirectory = Ar.ReadFString();

            if (LegacyVersion >= EOnDemandTocVersion.HostGroupName)
                HostGroupName = Ar.ReadFString();

            if (LegacyVersion < EOnDemandTocVersion.TocFlags)
                Flags = EOnDemandTocFlags.None;
        }
        else
        {
            Ar.Position = 0;

            if (!Ar.ReadBytes(16).SequenceEqual(NewExpectedMagic))
                throw new ParserException(Ar, "Invalid TOC signature found");

            Version = new FOnDemandTocVersion(Ar);
            if (!Version.IsValid())
                throw new ParserException(Ar, "Invalid TOC version found");

            Ar.Read<uint>(); // Pad
            EpochTimestamp = Ar.Read<long>();

            BuildVersionEntry = new FOnDemandStringEntry(Ar);
            TargetPlatformEntry = new FOnDemandStringEntry(Ar);
            ChunksDirectoryEntry = new FOnDemandStringEntry(Ar);
            HostGroupNameEntry = new FOnDemandStringEntry(Ar);
            CompressionFormatEntry = new FOnDemandStringEntry(Ar);

            StringTableLen = Ar.Read<uint>();
            ContainerCount = Ar.Read<uint>();
            Ar.Position += 48; // Pad2

            StringTable = Ar.ReadBytes((int)StringTableLen);

            BuildVersion = GetString(BuildVersionEntry);
            TargetPlatform = GetString(TargetPlatformEntry);
            ChunksDirectory = GetString(ChunksDirectoryEntry);
            HostGroupName = GetString(HostGroupNameEntry);
            CompressionFormat = GetString(CompressionFormatEntry);
        }
    }
}


public enum EOnDemandTocVersion : uint
{
    Invalid			= 0,
    Initial			= 1,
    UTocHash		= 2,
    BlockHash32		= 3,
    NoRawHash		= 4,
    Meta			= 5,
    ContainerId		= 6,
    AdditionalFiles	= 7,
    TagSets			= 8,
    ContainerFlags	= 9,
    TocFlags		= 10,
    HostGroupName	= 11,
    ContainerHeader	= 12,

    LatestPlusOne,
    Latest			= (LatestPlusOne - 1)
}

[Flags]
public enum EOnDemandTocFlags : uint
{
    None			= 0,
    InstallOnDemand	= (1 << 0),
    StreamOnDemand	= (1 << 1),

    Last			= StreamOnDemand
}

public enum EOnDemandTocMajorVersion : ushort
{
    Invalid = 0,
    One = 1,
    LatestPlusOne,
    Latest = LatestPlusOne - 1
}

public enum EOnDemandTocMinorVersion : ushort
{
    Invalid = 0,
    MemoryMapped = 1,
    LatestPlusOne,
    Latest = LatestPlusOne - 1
}

public class FOnDemandTocVersion
{
    public readonly EOnDemandTocMajorVersion Major;
    public readonly EOnDemandTocMinorVersion Minor;
    public bool IsValid() => Major > 0 && Minor > 0;

    public FOnDemandTocVersion(FArchive Ar)
    {
        Major = Ar.Read<EOnDemandTocMajorVersion>();
        Minor = Ar.Read<EOnDemandTocMinorVersion>();
    }
}

public class FOnDemandStringEntry
{
    public readonly uint Offset;
    public readonly uint Len;

    public FOnDemandStringEntry(FArchive Ar)
    {
        Offset = Ar.Read<uint>();
        Len = Ar.Read<uint>();
    }
}
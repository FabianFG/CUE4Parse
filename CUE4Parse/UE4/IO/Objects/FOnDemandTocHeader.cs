using System;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandTocHeader
{
    public const ulong ExpectedMagic = 0x6f6e64656d616e64; // ondemand

    public readonly ulong Magic;
    public readonly EOnDemandTocVersion Version;
    public readonly EOnDemandTocFlags Flags;
    public readonly uint BlockSize;
    public readonly string CompressionFormat;
    public readonly string ChunksDirectory;
    public string HostGroupName;

    public FOnDemandTocHeader(FArchive Ar)
    {
        Magic = Ar.Read<ulong>();
        if (Magic != ExpectedMagic)
            throw new ParserException(Ar, "Invalid iochunktoc magic");

        Version = Ar.Read<EOnDemandTocVersion>();
        if (Version == EOnDemandTocVersion.Invalid)
            throw new ParserException(Ar, "Invalid iochunktoc version");

        Flags = Ar.Read<EOnDemandTocFlags>();
        BlockSize = Ar.Read<uint>();
        CompressionFormat = Ar.ReadFString();
        ChunksDirectory = Ar.ReadFString();

        if (Version >= EOnDemandTocVersion.HostGroupName)
            HostGroupName = Ar.ReadFString();

        if (Version < EOnDemandTocVersion.TocFlags)
            Flags = EOnDemandTocFlags.None;
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

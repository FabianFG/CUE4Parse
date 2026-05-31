using System;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandTocHeader
{
    public ulong Magic;
    public EOnDemandTocVersion Version;
    public EOnDemandTocFlags Flags;
    public uint BlockSize;
    public string CompressionFormat;
    public string ChunksDirectory;
    public string HostGroupName;

    private const ulong _expectedMagic = 0x6f6e64656d616e64;
    
    public FOnDemandTocHeader(FArchive Ar)
    {
        Magic = Ar.Read<ulong>();
        if (Magic != _expectedMagic)
            throw new ParserException("Invalid FOnDemandTocHeader File Magic");

        Version = Ar.Read<EOnDemandTocVersion>();
        if (Version == EOnDemandTocVersion.Invalid || Version >= EOnDemandTocVersion.LatestPlusOne)
            throw new ParserException("Invalid FOnDemandTocHeader Version");
        
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
};

[Flags]
public enum EOnDemandTocFlags
{
    None			= 0,
    InstallOnDemand	= (1 << 0),
    StreamOnDemand	= (1 << 1),

    Last			= StreamOnDemand
};
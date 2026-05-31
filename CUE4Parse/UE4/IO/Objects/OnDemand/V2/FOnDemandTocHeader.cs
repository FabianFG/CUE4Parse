using System;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandTocHeader
{
    public FOnDemandTocSignature Signature;
    public FOnDemandTocVersion Version;
    public DateTime EpochTimeStamp;
    public FOnDemandStringEntry BuildVersion;
    public FOnDemandStringEntry TargetPlatform;
    public FOnDemandStringEntry ChunksDirectory;
    public FOnDemandStringEntry HostGroupName;
    public FOnDemandStringEntry CompressionFormat;
    public uint StringTableLength;
    public uint ContainerCount;
    
    public FOnDemandTocHeader(FArchive Ar)
    {
        Signature = new FOnDemandTocSignature(Ar);
        if (!Signature.IsValid()) throw new ParserException("Invalid FOnDemandTocHeader Signature");
        
        Version = Ar.Read<FOnDemandTocVersion>(); 
        if (!Version.IsValid()) throw new ParserException("Invalid FOnDemandTocHeader Toc Version");

        Ar.Position += sizeof(uint); // Pad
        EpochTimeStamp = DateTimeOffset.FromUnixTimeSeconds(Ar.Read<long>()).UtcDateTime;

        BuildVersion = Ar.Read<FOnDemandStringEntry>();
        TargetPlatform = Ar.Read<FOnDemandStringEntry>();
        ChunksDirectory = Ar.Read<FOnDemandStringEntry>();
        HostGroupName = Ar.Read<FOnDemandStringEntry>();
        CompressionFormat = Ar.Read<FOnDemandStringEntry>();
        
        StringTableLength = Ar.Read<uint>();
        ContainerCount = Ar.Read<uint>();

        Ar.Position += 48 * sizeof(byte); // Pad2
    }
}
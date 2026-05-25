using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using FIoBlockHash = uint;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandContainerEntry : IOnDemandContainerEntry
{
    public FIoContainerId ContainerId;
    public string ContainerName { get; }
    public string EncryptionKeyGuid;
    public FOnDemandTocEntry[] Entries;
    public uint[] BlockSizes;
    public FIoBlockHash[] BlockHashes;
    public FSHAHash UTocHash { get; }
    public EOnDemandContainerFlags ContainerFlags;
    
    public FOnDemandContainerEntry(FArchive Ar, EOnDemandTocVersion version)
    {
        if (version >= EOnDemandTocVersion.ContainerId)
            ContainerId = Ar.Read<FIoContainerId>();
        
        ContainerName = Ar.ReadFString();
        EncryptionKeyGuid = Ar.ReadFString();
        Entries = Ar.ReadArray(() => new FOnDemandTocEntry(Ar));
        BlockSizes = Ar.ReadArray<uint>();
        BlockHashes = Ar.ReadArray<FIoBlockHash>();
        UTocHash = new FSHAHash(Ar);
        
        if (version >= EOnDemandTocVersion.ContainerFlags)
            ContainerFlags = Ar.Read<EOnDemandContainerFlags>();
        
        if (version >= EOnDemandTocVersion.ContainerHeader)
            Ar.SkipFixedArray(sizeof(byte)); // Header
    }

    public bool TryGetFileEntryHash(FIoChunkId chunkId, out FSHAHash fileEntryHash)
    {
        foreach (var entry in Entries)
        {
            if (chunkId != entry.ChunkId) 
                continue;
            
            fileEntryHash = entry.Hash;
            return true;
        }

        fileEntryHash = default;
        return false;
    }
}

[Flags]
public enum EOnDemandContainerFlags : byte
{
    None					= 0,
    PendingEncryptionKey	= (1 << 0),
    Mounted					= (1 << 1),
    StreamOnDemand			= (1 << 2),
    InstallOnDemand			= (1 << 3),
    Encrypted				= (1 << 4),
    WithSoftReferences		= (1 << 5),
    PendingHostGroup		= (1 << 6),
    Last = PendingHostGroup
}
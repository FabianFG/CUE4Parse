using System;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandContainerEntry : IOnDemandContainerEntry
{
    public FGuid EncryptionKeyGuid;
    public FIoContainerId ContainerId;
    public string ContainerName { get; }
    public uint ContainerHeaderSize;
    public uint DataOffset;
    public uint DataSize;
    public uint ChunkCount;
    public uint BlockCount;
    public uint BlockSize;
    public uint TagSetCount;
    public uint TagSetIndicesCount;
    public FSHAHash UTocHash { get; }
    public EOnDemandContainerEntryFlags ContainerFlags;
    public EIoContainerFlags FileContainerFlags;
    public uint PartitionCount;

    public FIoChunkId[]? ChunkIds { get; set; }
    public FOnDemandChunkEntry[]? ChunkEntries { get; set; }
    public FOnDemandPartitionEntry[]? PartitionEntries { get; set; }
    
    public FOnDemandContainerEntry(FArchive Ar, Func<FOnDemandStringEntry, string> func)
    {
        EncryptionKeyGuid = Ar.Read<FGuid>();
        ContainerId = Ar.Read<FIoContainerId>();
        ContainerName = func(Ar.Read<FOnDemandStringEntry>());
        ContainerHeaderSize = Ar.Read<uint>();
        DataOffset = Ar.Read<uint>();
        DataSize = Ar.Read<uint>();
        ChunkCount = Ar.Read<uint>();
        BlockCount = Ar.Read<uint>();
        BlockSize = Ar.Read<uint>();
        TagSetCount = Ar.Read<uint>();
        TagSetIndicesCount = Ar.Read<uint>();
        UTocHash = new FSHAHash(Ar);
        ContainerFlags = Ar.Read<EOnDemandContainerEntryFlags>();
        FileContainerFlags = Ar.Read<EIoContainerFlags>();
        PartitionCount = Ar.Read<uint>();

        Ar.Position += 32;
    }

    public bool TryGetFileEntryHash(FIoChunkId chunkId, out FSHAHash fileEntryHash)
    {
        if (ChunkIds == null || ChunkEntries == null)
        {
            fileEntryHash = default;
            return false;
        }
        
        var index = Array.IndexOf(ChunkIds, chunkId);
        if (index == -1)
        {
            fileEntryHash = default;
            return false;
        }

        fileEntryHash = ChunkEntries[index].Hash;
        return true;
    }
}

[Flags]
public enum EOnDemandContainerEntryFlags : uint
{
    None			= 0,
    InstallOnDemand	= (1 << 0),
    StreamOnDemand	= (1 << 1),

    Last			= StreamOnDemand
}
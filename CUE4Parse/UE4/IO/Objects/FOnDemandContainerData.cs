using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using FIoBlockHash = uint;

namespace CUE4Parse.UE4.IO.Objects;

public class FOnDemandContainerData
{
    public readonly FIoChunkId[] ChunkIds;
    public readonly FOnDemandChunkEntry[] ChunkEntries;
    public readonly uint[] BlockSizes;
    public readonly FIoBlockHash[] BlockHashes;
    public readonly FOnDemandTagSetEntry[] TagSets;
    public readonly uint[] TagSetIndices;
    public readonly FIoContainerHeader ContainerHeader;

    public FOnDemandContainerData(FArchive Ar, FOnDemandTocContainerEntry entry)
    {
        ChunkIds = Ar.ReadArray<FIoChunkId>((int)entry.ChunkCount);
        ChunkEntries = Ar.ReadArray((int)entry.ChunkCount, () => new FOnDemandChunkEntry(Ar));
        BlockSizes = Ar.ReadArray<uint>((int)entry.BlockCount);
        BlockHashes = Ar.ReadArray<FIoBlockHash>((int)entry.BlockCount);
        TagSets = Ar.ReadArray((int)entry.TagSetCount, () => new FOnDemandTagSetEntry(Ar));
        TagSetIndices = Ar.ReadArray<uint>((int)entry.TagSetIndicesCount);

        var End = Ar.Position + entry.ContainerHeaderSize;

        ContainerHeader = new FIoContainerHeader(Ar);

        Ar.Position = End;
    }
}

public class FOnDemandChunkBlockInfo
{
    public readonly uint OffsetOrSize;
    public readonly bool bHasOffset;
    public readonly uint CountOrHash;

    public uint Offset => bHasOffset ? OffsetOrSize : ~0u;
    public uint Count => bHasOffset ? CountOrHash : OffsetOrSize > 0 ? 1u : 0u;
    public uint Size => bHasOffset ? 0 : OffsetOrSize;
    public uint Hash => bHasOffset ? 0 : CountOrHash;

    public FOnDemandChunkBlockInfo(FArchive Ar)
    {
        var Raw = Ar.Read<uint>();
        bHasOffset = (Raw & 0x80000000u) != 0;
        OffsetOrSize = Raw & 0x7FFFFFFFu;
        CountOrHash = Ar.Read<uint>();
    }
}

public class FOnDemandChunkEntry
{
    public readonly FSHAHash Hash;
    public readonly uint RawSize;
    public readonly uint EncodedSize;
    public readonly FOnDemandChunkBlockInfo BlockInfo;

    public uint GetDiskSize() => (uint)EncodedSize.Align(Aes.ALIGN);

    public FOnDemandChunkEntry(FArchive Ar)
    {
        Hash = new FSHAHash(Ar);
        RawSize = Ar.Read<uint>();
        EncodedSize = Ar.Read<uint>();
        BlockInfo = new FOnDemandChunkBlockInfo(Ar);
    }
}

public class FOnDemandTagSetEntry
{
    public readonly FOnDemandStringEntry Tag;
    public readonly uint Offset;
    public readonly uint Count;

    public FOnDemandTagSetEntry(FArchive Ar)
    {
        Tag = new FOnDemandStringEntry(Ar);
        Offset = Ar.Read<uint>();
        Count = Ar.Read<uint>();
    }
}
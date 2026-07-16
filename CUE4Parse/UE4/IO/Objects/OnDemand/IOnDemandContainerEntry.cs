using CUE4Parse.UE4.IO.Objects.OnDemand.V2;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.IO.Objects.OnDemand;

public interface IOnDemandContainerEntry
{
    public FSHAHash UTocHash { get; }
    public string ContainerName { get; }

    public bool TryGetFileEntryHash(FIoChunkId chunkId, out FOnDemandFileEntry fileEntry);
}

public readonly struct FOnDemandFileEntry(FSHAHash fileEntryHash, uint? partitionOffset = null, string? chunkExt = null)
{
    public readonly FSHAHash FileEntryHash = fileEntryHash;
    public readonly uint PartitionOffset = partitionOffset ?? 0;
    public readonly string ChunkExt = chunkExt ?? "iochunk";

    public FOnDemandFileEntry(FOnDemandChunkEntry entry) : this(entry.Hash, entry.PartitionOffset)
    {

    }
}

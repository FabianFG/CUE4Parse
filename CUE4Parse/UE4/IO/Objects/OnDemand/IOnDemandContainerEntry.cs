using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.IO.Objects.OnDemand;

public interface IOnDemandContainerEntry
{
    public FSHAHash UTocHash { get; }
    public string ContainerName { get; }

    public bool TryGetFileEntryHash(FIoChunkId chunkId, out FSHAHash fileEntryHash);
}
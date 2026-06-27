using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandToc : IOnDemandToc
{
    private string? _chunksDirectory;
    public string ChunksDirectory => _chunksDirectory ??= GetOnDemandString(_header.ChunksDirectory);

    public IReadOnlyList<IOnDemandContainerEntry> Containers => _containerEntries;

    private readonly FOnDemandTocHeader _header;
    private readonly byte[] _stringTable;
    private readonly FOnDemandContainerEntry[] _containerEntries;

    public FOnDemandToc(FArchive Ar)
    {
        _header = new FOnDemandTocHeader(Ar);
        _stringTable = Ar.ReadBytes((int) _header.StringTableLength);
        _containerEntries = Ar.ReadArray((int) _header.ContainerCount, () => new FOnDemandContainerEntry(Ar, GetOnDemandString));

        var savedPos = Ar.Position;
        foreach (var container in _containerEntries)
        {
            Ar.Position = savedPos + container.DataOffset;

            container.PartitionEntries = Ar.ReadArray((int) container.PartitionCount, () => new FOnDemandPartitionEntry(Ar));
            container.ChunkIds = Ar.ReadArray<FIoChunkId>((int) container.ChunkCount);
            container.ChunkEntries = Ar.ReadArray((int) container.ChunkCount, () => new FOnDemandChunkEntry(Ar, _header.Version.Minor));
        }
    }

    private string GetOnDemandString(FOnDemandStringEntry stringEntry) =>
        Encoding.UTF8.GetString(_stringTable.AsSpan((int)stringEntry.Offset, (int)stringEntry.Length));
}

using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Tencent.AE.Repo;

public class RepoIndex
{
    public long Size;
    public long Indices;
    public long ZSize;
    public int ChunkSize;
    public CompressionMethod CompressionMethod;
    public ushort Map;

    public int[] ChunkSizes;
    
    public RepoIndex(FArchive Ar)
    {
        Size = Ar.Read<long>();
        Indices = Ar.Read<long>();
        ZSize = Ar.Read<long>();
        ChunkSize = Ar.Read<int>();
        CompressionMethod = Ar.Read<ushort>() switch
        {
            1 => CompressionMethod.LZ4,
            4 => CompressionMethod.Oodle,
            _ => CompressionMethod.None
        };
        Map = Ar.Read<ushort>();
        
        var chunks = (int) Size / ChunkSize;
        if (Size % ChunkSize != 0) chunks++;
        ChunkSizes = Ar.ReadArray<int>(chunks);

        var offset = Ar.Position;
        
    }
}
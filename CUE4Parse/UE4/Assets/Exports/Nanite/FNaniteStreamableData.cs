using System.IO;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FNaniteStreamableData
{
    public FFixupChunk FixupChunk;
    /// <summary>Describes the contents of this page.</summary>
    public FPageDiskHeader PageDiskHeader;
    /// <summary>A list of headers that </summary>
    public FClusterDiskHeader[] ClusterDiskHeaders;
    // ignoring because they take up so much space in the JSON and aren't really usefull to the end user, would probably be nice to put behind a flag
    [JsonIgnore] public FPageGPUHeader PageGPUHeader;
    [JsonIgnore] public FCluster[] Clusters = [];

    // state variables used for parsing, this isn't serialized.
    [JsonIgnore] public long PageDiskHeaderOffset = -1;
    [JsonIgnore] public long GPUPageHeaderOffset = -1;
    [JsonIgnore] public readonly int NumClusters;

    public FNaniteStreamableData(FByteArchive Ar, FNaniteResources resources, uint pageIndex)
    {
        FixupChunk = new FFixupChunk(Ar);
        // origin of all the offsets in the page cluster header
        PageDiskHeaderOffset = Ar.Position;
        PageDiskHeader = new FPageDiskHeader(Ar);
        if (PageDiskHeader.NumClusters > NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE(Ar.Game))
        {
            throw new InvalidDataException($"Too many clusters in FNaniteStreamableData, {PageDiskHeader.NumClusters} max is {NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE(Ar.Game)}");
        }
        if (PageDiskHeader.NumTexCoords > NaniteConstants.NANITE_MAX_UVS)
        {
            throw new InvalidDataException($"Too many tex coords in FNaniteStreamableData, {PageDiskHeader.NumTexCoords} max is {NaniteConstants.NANITE_MAX_UVS}");
        }
        if (PageDiskHeader.NumClusters != FixupChunk.Header.NumClusters)
        {
            throw new InvalidDataException($"Data corruption detected! page disk header and fixup cluster do not agree on the number of clusters. {PageDiskHeader.NumClusters} vs {FixupChunk.Header.NumClusters}");
        }

        NumClusters = (int) PageDiskHeader.NumClusters;
        ClusterDiskHeaders = Ar.ReadArray(NumClusters, () => new FClusterDiskHeader(Ar));

        GPUPageHeaderOffset = Ar.Position;
        PageGPUHeader = Ar.Read<FPageGPUHeader>();
        if (PageGPUHeader.NumClusters != FixupChunk.Header.NumClusters)
        {
            throw new InvalidDataException($"Data corruption detected! page gpu header and fixup cluster do not agree on the number of clusters. {PageGPUHeader.NumClusters} vs {FixupChunk.Header.NumClusters}");
        }

        // Clusters are stored in SOA layout to speedup GPU transcoding
        var clusterOrigin = Ar.Position;
        var stride = 16 * (NumClusters - 1);
        Clusters = new FCluster[NumClusters];
        for (uint clusterIndex = 0; clusterIndex < NumClusters; clusterIndex++)
        {
            Ar.Position = clusterOrigin + 16 * clusterIndex;
            Clusters[clusterIndex] = new FCluster(Ar, stride);
        }

        for (uint clusterIndex = 0; clusterIndex < NumClusters; clusterIndex++)
        {
            Clusters[clusterIndex].Decode(Ar, this, clusterIndex);
        }

        // resolve vertex references once all basic data is parsed
        for (uint clusterIndex = 0; clusterIndex < Clusters.Length; clusterIndex++)
        {
            Clusters[clusterIndex].ResolveVertexReferences(Ar, resources, this, ClusterDiskHeaders[clusterIndex], resources.PageStreamingStates[pageIndex]);
        }
    }
}

/// <summary>A header that describes the cluster at the same index as itself.</summary>
public readonly struct FClusterDiskHeader
{
    /// <summary>The offset from the disk page header where the triangle indices for this cluster starts.</summary>
    public readonly uint IndexDataOffset;
    /// <summary>The offset from the disk page header the reference vertex page mapping for this cluster starts.</summary>
    public readonly uint PageClusterMapOffset;
    /// <summary>The offset from the disk page header the reference vertex data for this cluster starts.</summary>
    public readonly uint VertexRefDataOffset;

    /// <summary>Added in 5.4</summary>
    public readonly uint LowBytesDataOffset;
    /// <summary>Added in 5.4</summary>
    public readonly uint MidBytesDataOffset;
    /// <summary>Added in 5.4</summary>
    public readonly uint HighBytesDataOffset;

    /// <summary>The offset from the disk page header the non-ref vertex's positions for this cluster starts. Removed after 5.3.</summary>
    public readonly uint PositionDataOffset;
    /// <summary>The offset from the disk page header the non-ref vertex's attributes for this cluster starts. Removed after 5.3.</summary>
    public readonly uint AttributeDataOffset;

    /// <summary>The number of vertexes that are references in this cluster.</summary>
    public readonly uint NumVertexRefs;
    public readonly uint NumPrevRefVerticesBeforeDwords;
    public readonly uint NumPrevNewVerticesBeforeDwords;

    public FClusterDiskHeader(FArchive Ar)
    {
        IndexDataOffset = Ar.Read<uint>();
        PageClusterMapOffset = Ar.Read<uint>();
        VertexRefDataOffset = Ar.Read<uint>();

        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            LowBytesDataOffset = Ar.Read<uint>();
            MidBytesDataOffset = Ar.Read<uint>();
            HighBytesDataOffset = Ar.Read<uint>();
        }
        else
        {
            PositionDataOffset = Ar.Read<uint>();
            AttributeDataOffset = Ar.Read<uint>();
        }
        NumVertexRefs = Ar.Read<uint>();
        NumPrevRefVerticesBeforeDwords = Ar.Read<uint>();
        NumPrevNewVerticesBeforeDwords = Ar.Read<uint>();
    }
}

/// <summary>A header that is directly transcoded to the gpu at runtime.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct FPageGPUHeader
{
    public readonly uint NumClusters_MaxClusterBoneInfluences_MaxVoxelBoneInfluences;
    [JsonIgnore] public readonly uint Pad1;
    [JsonIgnore] public readonly uint Pad2;
    [JsonIgnore] public readonly uint Pad3;

    public uint NumClusters => NaniteUtils.GetBits(NumClusters_MaxClusterBoneInfluences_MaxVoxelBoneInfluences, 16, 0);
    public uint MaxClusterBoneInfluences => NaniteUtils.GetBits(NumClusters_MaxClusterBoneInfluences_MaxVoxelBoneInfluences, 8, 16);
    public uint MaxVoxelBoneInfluences => NaniteUtils.GetBits(NumClusters_MaxClusterBoneInfluences_MaxVoxelBoneInfluences, 8, 24);
}

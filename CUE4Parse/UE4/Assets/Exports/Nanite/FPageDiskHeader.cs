using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

/// <summary>A header that describes the contents of a nanite streaming page.</summary>
public readonly struct FPageDiskHeader
{
    /// <summary>The number of bytes that should follow the gpu header. Removed after 5.3.</summary>
    public readonly uint GpuSize;
    /// <summary>The number of cluster present in this page.</summary>
    public readonly uint NumClusters;
    /// <summary>The bumber of uint4s used to store the cluster data, the material tables and the uv decode info.</summary>
    public readonly uint NumRawFloat4s;
    /// <summary>The number of UVs this cluster uses. Removed after 5.3.</summary>
    public readonly uint NumTexCoords;
    /// <summary>The number of vertex references withing this page.</summary>
    public readonly uint NumVertexRefs;
    /// <summary>The offset from this header where the uv ranges can be found.</summary>
    public readonly uint DecodeInfoOffset;
    /// <summary>The offset from this header where the tirangle strip bitmasks start.</summary>
    public readonly uint StripBitmaskOffset;
    /// <summary>The offset from this header where the reference vertex bitmasks start.</summary>
    public readonly uint VertexRefBitmaskOffset;

    public FPageDiskHeader(FArchive Ar)
    {
        if (Ar.Game <= EGame.GAME_UE5_3) GpuSize = Ar.Read<uint>();
        NumClusters = Ar.Read<uint>();
        NumRawFloat4s = Ar.Read<uint>();
        if (Ar.Game <= EGame.GAME_UE5_3) NumTexCoords = Ar.Read<uint>();
        NumVertexRefs = Ar.Read<uint>();
        DecodeInfoOffset = Ar.Read<uint>();
        StripBitmaskOffset = Ar.Read<uint>();
        VertexRefBitmaskOffset = Ar.Read<uint>();
    }
}

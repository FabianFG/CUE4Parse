using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Landscape;

public class ULandscapeHeightmapTextureEdgeFixup : UAssetUserData
{
    public FHeightmapTextureEdgeSnapshot EdgeSnapshot;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        EdgeSnapshot = new FHeightmapTextureEdgeSnapshot(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(EdgeSnapshot));
        serializer.Serialize(writer, EdgeSnapshot);
    }
}

public class FHeightmapTextureEdgeSnapshot
{
    public const int EdgeHashesLength = 8;
    // edge length for recorded edge data here - when up to date, should match texture resolution (width AND height)
    public int EdgeLength;
    // height and normal data for each edge & mip (in heightmap texture format 32bpp). Use GetEdgeData() to access specific edges and mips.
    public uint[] EdgeData;
    public uint[] CornerData = [];
    // hash of each edge / corner (at full resolution) in the EdgeData
    public uint[] SnapshotEdgeHashes;
    // hash of each edge / corner (at full resolution) in the GPU Texture Resource (at initial unpatched state)
    public uint[] InitialEdgeHashes = [];

    public FHeightmapTextureEdgeSnapshot(FAssetArchive Ar)
    {
        EdgeLength = Ar.Read<int>();
        EdgeData = Ar.ReadArray<uint>();
        if (FHeightmapTextureEdgeSnapshotCustomVersion.Get(Ar) <= FHeightmapTextureEdgeSnapshotCustomVersion.Type.BeforeCornerDataWasRemoved)
        {
            CornerData = Ar.ReadArray<uint>(4);
        }
        SnapshotEdgeHashes = Ar.ReadArray<uint>(EdgeHashesLength);

        if (FHeightmapTextureEdgeSnapshotCustomVersion.Get(Ar) > FHeightmapTextureEdgeSnapshotCustomVersion.Type.BeforeInitialHashWasAdded)
        {
            InitialEdgeHashes = Ar.ReadArray<uint>(EdgeHashesLength);
        }
    }
}

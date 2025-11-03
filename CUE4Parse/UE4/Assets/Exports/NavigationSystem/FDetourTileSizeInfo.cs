using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

public class FDetourTileSizeInfo
{
    public ushort VertCount;
    public ushort PolyCount;
    public ushort MaxLinkCount;
    public ushort DetailMeshCount;
    public ushort DetailVertCount;
    public ushort DetailTriCount; 
    public ushort BvNodeCount;
    public ushort OffMeshConCount;
    public ushort OffMeshSegConCount;
    public ushort ClusterCount;
    public ushort OffMeshBase;
    
    public FDetourTileSizeInfo(FArchive Ar)
    {
        VertCount = Ar.Read<ushort>();
        PolyCount = Ar.Read<ushort>();
        MaxLinkCount = Ar.Read<ushort>();
        DetailMeshCount = Ar.Read<ushort>();
        DetailVertCount = Ar.Read<ushort>();
        DetailTriCount = Ar.Read<ushort>();
        BvNodeCount = Ar.Read<ushort>();
        OffMeshConCount = Ar.Read<ushort>();
        OffMeshSegConCount = Ar.Read<ushort>();
        ClusterCount = Ar.Read<ushort>();

        OffMeshBase = DetailMeshCount;
    }
}
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem;

public class FDetourTileSizeInfo
{
    public int VertCount;
    public int PolyCount;
    public int MaxLinkCount;
    public int DetailMeshCount;
    public int DetailVertCount;
    public int DetailTriCount; 
    public int BvNodeCount;
    public int OffMeshConCount;
    public int OffMeshSegConCount;
    public int ClusterCount;
    public int OffMeshBase;
    
    public FDetourTileSizeInfo(FArchive Ar)
    {
        if (Ar.Game >= Versions.EGame.GAME_UE5_0)
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
        }
        else
        {
            VertCount = Ar.Read<int>();
            PolyCount = Ar.Read<int>();
            MaxLinkCount = Ar.Read<int>();
            DetailMeshCount = Ar.Read<int>();
            DetailVertCount = Ar.Read<int>();
            DetailTriCount = Ar.Read<int>();
            BvNodeCount = Ar.Read<int>();
            OffMeshConCount = Ar.Read<int>();
            OffMeshSegConCount = Ar.Read<int>();
            ClusterCount = Ar.Read<int>();
        }

        OffMeshBase = DetailMeshCount;
    }
}

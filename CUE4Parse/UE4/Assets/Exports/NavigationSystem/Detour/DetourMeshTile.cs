using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public class DetourMeshTile
{
    public DetourMeshHeader Header;
    public FVector[] Vertices;
    public DetourPoly[] Polys;
    public DetourPolyDetail[] DetailMeshes;
    public FVector[] DetailVertices;
    public byte[][] DetailTris;
    public DetourBVNode[] BvTree;
    public DetourOffMeshConnection[] OffMeshConnections;
    public DetourOffMeshSegmentConnection[] OffMeshSegments;
    public FVector[] Clusters;
    public ushort[] PolyClusters;

    public DetourMeshTile(FArchive Ar, FDetourTileSizeInfo sizeInfo, ENavMeshVersion navMeshVersion)
    {
        var polyClusterCount = sizeInfo.OffMeshBase;

        Header = new DetourMeshHeader(Ar, navMeshVersion);
        if (navMeshVersion < ENavMeshVersion.NAVMESHVER_TILEVERT_ENCODING)
        {
            // Old format is in world-space (FVector::FReal * 3 per vertex).
            Vertices = Ar.ReadArray(sizeInfo.VertCount, () => new FVector(Ar));
        }
        else
        {
            // New format: tile-local floats stored directly in dtTileVert.
            Vertices = Ar.ReadArray<FVector>(sizeInfo.VertCount);
        }

        Polys = Ar.ReadArray(sizeInfo.PolyCount, () => new DetourPoly(Ar));
        DetailMeshes = Ar.ReadArray(sizeInfo.DetailMeshCount, () => new DetourPolyDetail(Ar));
        if (navMeshVersion < ENavMeshVersion.NAVMESHVER_TILEVERT_ENCODING)
        {
            // Old format is in world-space (FVector::FReal * 3 per vertex).
            DetailVertices = Ar.ReadArray(sizeInfo.DetailVertCount, () => new FVector(Ar));
        }
        else
        {
            // New format: tile-local floats stored directly in dtTileVert.
            DetailVertices = Ar.ReadArray<FVector>(sizeInfo.DetailVertCount);
        }

        DetailTris = new byte[sizeInfo.DetailTriCount][];
        for (var i = 0; i < DetailTris.Length; i++)
            DetailTris[i] = Ar.ReadArray<byte>(4);

        if (Ar.Game is GAME_MongilStarDive) Ar.Position += sizeInfo.PolyCount;

        BvTree = Ar.ReadArray(sizeInfo.BvNodeCount, () => new DetourBVNode(Ar));
        OffMeshConnections = Ar.ReadArray(sizeInfo.OffMeshConCount, () => new DetourOffMeshConnection(Ar));

        if (navMeshVersion >= ENavMeshVersion.NAVMESHVER_OFFMESH_HEIGHT_BUG)
        {
            for (var i = 0; i < OffMeshConnections.Length; i++)
                OffMeshConnections[i].Height = Ar.ReadFReal();
        }

        OffMeshSegments = Ar.ReadArray(sizeInfo.OffMeshSegConCount, () => new DetourOffMeshSegmentConnection(Ar));
        Clusters = Ar.ReadArray(sizeInfo.ClusterCount, () => new FVector(Ar));
        PolyClusters = Ar.ReadArray<ushort>(polyClusterCount);
    }
}

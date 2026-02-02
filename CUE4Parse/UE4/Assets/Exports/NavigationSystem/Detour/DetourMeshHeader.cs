using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourMeshHeader
{
    public int Version;
    public int X;
    public int Y;
    public int Layer;
    public int PolyCount;
    public int VertCount;
    public int MaxLinkCount;
    public int DetailMeshCount;
    public int DetailVertCount;
    public int DetailTriCount;
    public int BvNodeCount;
    public int OffMeshConCount;
    public int OffMeshBase;
    public byte Resolution;
    public FVector BMin;
    public FVector BMax;
    public int ClusterCount;
    public int OffMeshSegConCount;
    public int OffMeshSegPolyBase;
    public int OffMeshSegVertBase;
    public float walkableHeight; // The height of the agents using the tile.
    public float walkableRadius; // The radius of the agents using the tile.
    public float walkableClimb; // The maximum climb height of the agents using the tile.
    public float bvQuantFactor; // The bounding volume quantization factor. 


    public DetourMeshHeader(FArchive Ar, ENavMeshVersion navMeshVersion)
    {
        if (Ar.Game >= Versions.EGame.GAME_UE5_0)
        {
            Version = Ar.Read<ushort>();
            X = Ar.Read<int>();
            Y = Ar.Read<int>();

            Layer = Ar.Read<ushort>();
            PolyCount = Ar.Read<ushort>();
            VertCount = Ar.Read<ushort>();

            MaxLinkCount = Ar.Read<ushort>();
            DetailMeshCount = Ar.Read<ushort>();
            DetailVertCount = Ar.Read<ushort>();
            DetailTriCount = Ar.Read<ushort>();

            BvNodeCount = Ar.Read<ushort>();
            OffMeshConCount = Ar.Read<ushort>();
            OffMeshBase = Ar.Read<ushort>();

            if (navMeshVersion >= ENavMeshVersion.NAVMESHVER_TILE_RESOLUTIONS)
                Resolution = Ar.Read<byte>();

            BMin = new FVector(Ar);
            BMax = new FVector(Ar);
            ClusterCount = Ar.Read<ushort>();

            OffMeshSegConCount = Ar.Read<ushort>();
            OffMeshSegPolyBase = Ar.Read<ushort>();
            OffMeshSegVertBase = Ar.Read<ushort>();
        }
        else
        {
            var Magic = Ar.Read<uint>();
            Version = Ar.Read<int>();
            X = Ar.Read<int>();
            Y = Ar.Read<int>();

            Layer = Ar.Read<int>();
            var userId = Ar.Read<uint>();
            PolyCount = Ar.Read<int>();
            VertCount = Ar.Read<int>();

            MaxLinkCount = Ar.Read<int>();
            DetailMeshCount = Ar.Read<int>();
            DetailVertCount = Ar.Read<int>();
            DetailTriCount = Ar.Read<int>();

            BvNodeCount = Ar.Read<int>();
            OffMeshConCount = Ar.Read<int>();
            OffMeshBase = Ar.Read<int>();

            walkableHeight = Ar.ReadFReal();
            walkableRadius = Ar.ReadFReal();
            walkableClimb = Ar.ReadFReal();

            BMin = new FVector(Ar);
            BMax = new FVector(Ar);
            bvQuantFactor = Ar.ReadFReal();
            ClusterCount = Ar.Read<int>();

            OffMeshSegConCount = Ar.Read<int>();
            OffMeshSegPolyBase = Ar.Read<int>();
            OffMeshSegVertBase = Ar.Read<int>();
        }
    }
}

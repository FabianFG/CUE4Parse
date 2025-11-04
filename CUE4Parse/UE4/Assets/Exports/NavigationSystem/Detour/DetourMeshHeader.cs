using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourMeshHeader
{
    public ushort Version;
    public int X;
    public int Y;
    public ushort Layer;
    public ushort PolyCount;
    public ushort VertCount;
    public ushort MaxLinkCount;
    public ushort DetailMeshCount;
    public ushort DetailVertCount;
    public ushort DetailTriCount;
    public ushort BvNodeCount;
    public ushort OffMeshConCount;
    public ushort OffMeshBase;
    public byte Resolution;
    public FVector BMin;
    public FVector BMax;
    public ushort ClusterCount;
    public ushort OffMeshSegConCount;
    public ushort OffMeshSegPolyBase;
    public ushort OffMeshSegVertBase;
    
    public DetourMeshHeader(FArchive Ar, ENavMeshVersion navMeshVersion)
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

        if (navMeshVersion >= ENavMeshVersion.TileResolutions)
            Resolution = Ar.Read<byte>();

        BMin = new FVector(Ar);
        BMax = new FVector(Ar);
        ClusterCount = Ar.Read<ushort>();
        
        OffMeshSegConCount = Ar.Read<ushort>();
        OffMeshSegPolyBase = Ar.Read<ushort>();
        OffMeshSegVertBase = Ar.Read<ushort>();
    }
}
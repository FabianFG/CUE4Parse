using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourOffMeshSegmentConnection
{
    public FVector StartA;
    public FVector StartB;
    public FVector EndA;
    public FVector EndB;

    public float Radius;
    public ushort FirstPoly;
    public byte NumberOfPolys;
    public EDetourOffMesh Flags;
    public uint UserId;
    
    public DetourOffMeshSegmentConnection(FArchive Ar)
    {
        StartA = new FVector(Ar);
        StartB = new FVector(Ar);
        EndA = new FVector(Ar);
        EndB = new FVector(Ar);

        Radius = Ar.ReadFReal();
        FirstPoly = Ar.Read<ushort>();
        NumberOfPolys = Ar.Read<byte>();
        Flags = Ar.Read<EDetourOffMesh>();
        UserId = Ar.Read<uint>();
    }
}
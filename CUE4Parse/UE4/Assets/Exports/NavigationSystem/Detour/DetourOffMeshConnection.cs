using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourOffMeshConnection
{
    /// <summary>
    /// The endpoints of the connection. [(ax, ay, az, bx, by, bz)]
    /// </summary>
    public float[] Position;
    public float Radius;

    /// <summary>
    /// The snap height of endpoints (less than 0 = use step height)
    /// </summary>
    public float Height;
    
    /// <summary>
    /// The polygon reference of the connection within the tile.
    /// </summary>
    public ushort Poly;
    public EDetourOffMesh Flags;
    public byte Side;
    public ulong UserId;
    
    public DetourOffMeshConnection(FArchive Ar)
    {
        Position = Ar.ReadArray(6, Ar.ReadFReal);
        Radius = Ar.ReadFReal();
        Poly = Ar.Read<ushort>();
        Flags = Ar.Read<EDetourOffMesh>();
        Side = Ar.Read<byte>();

        UserId = FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.NavigationLinkID32To64
            ? Ar.Read<uint>()
            : Ar.Read<ulong>();
    }
}

[Flags]
public enum EDetourOffMesh : byte
{
    ConnectionBidir = 0x01,
    ConnectionPoint = 0x02,
    ConnectionSegment = 0x04,
    ConnectionCheapAre = 0x08,
    ConnectionGenerated = 0x10
}
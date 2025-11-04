using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourPoly
{
    public uint FirstLink;
    public ushort[] Verts;
    public ushort[] Neis;
    public ushort Flags;
    public byte VertCount;
    public byte AreaAndType;

    private const int DT_VERTS_PER_POLYGON = 6;
    
    public DetourPoly(FArchive Ar)
    {
        FirstLink = Ar.Read<uint>();
        Verts = Ar.ReadArray<ushort>(DT_VERTS_PER_POLYGON);
        Neis = Ar.ReadArray<ushort>(DT_VERTS_PER_POLYGON);
        Flags = Ar.Read<ushort>();
        VertCount = Ar.Read<byte>();
        AreaAndType = Ar.Read<byte>();
    }
}
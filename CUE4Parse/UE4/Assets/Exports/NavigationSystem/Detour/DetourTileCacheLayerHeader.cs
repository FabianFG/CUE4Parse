using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourTileCacheLayerHeader
{
    public short Version;
    public int Tx;
    public int Ty;
    public int TLayer;
    public FVector BMin;
    public FVector BMax;
    public ushort MinHeight;
    public ushort MaxHeight;
    public ushort Width;
    public ushort Height;
    public ushort MinX;
    public ushort MaxX;
    public ushort MinY;
    public ushort MaxY;

    public int FloatSize = Unsafe.SizeOf<DetourTileCacheLayerHeader>();
    public int DoubleSize = (2 * 3 * sizeof(double)) + (3 * sizeof(int)) + (9 * sizeof(short));
    
    public DetourTileCacheLayerHeader(FArchive Ar)
    {
        Version = Ar.Read<short>();
        Tx = Ar.Read<int>();
        Ty = Ar.Read<int>();
        TLayer = Ar.Read<int>();

        BMin.X = Ar.ReadFReal();
        BMax.X = Ar.ReadFReal();

        BMin.Y = Ar.ReadFReal();
        BMax.Y = Ar.ReadFReal();

        BMin.Z = Ar.ReadFReal();
        BMax.Z = Ar.ReadFReal();

        MinHeight = Ar.Read<ushort>();
        MaxHeight = Ar.Read<ushort>();

        Width = Ar.Read<ushort>();
        Height = Ar.Read<ushort>();
        
        MinX = Ar.Read<ushort>();
        MaxX = Ar.Read<ushort>();
        
        MinY = Ar.Read<ushort>();
        MaxY = Ar.Read<ushort>();
    }
}
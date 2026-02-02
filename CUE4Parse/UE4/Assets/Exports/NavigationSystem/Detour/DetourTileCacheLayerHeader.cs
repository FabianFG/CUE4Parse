using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.NavigationSystem.Detour;

public struct DetourTileCacheLayerHeader
{
    public int Version;
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

    public int Size(FArchive Ar)
    {
        var size = Unsafe.SizeOf<DetourTileCacheLayerHeader>();
        return Ar.Game switch
        {
            < EGame.GAME_UE5_0 => size + sizeof(int),
            _ when Ar.Ver < EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES => size - 2,
            _ => size + (6 * sizeof(float)) - 2
        };
    }
    
    public DetourTileCacheLayerHeader(FArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_0)
        {
            var magic = Ar.Read<uint>();
            Version = Ar.Read<int>();
        }
        else
        {
            Version = Ar.Read<short>();
        }
        
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

using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FLayoutBlock
{
    public readonly FIntVector2 Min;
    public readonly FIntVector2 Size;
    public readonly ulong Id;
    public readonly int Priority;

    private readonly uint Packed;

    public readonly bool bReduceBothAxes => (Packed & 1) != 0;
    public readonly bool bReduceByTwo  => (Packed & 2) != 0;

    public FLayoutBlock(FMutableArchive Ar, int version = 6)
    {
        if (Ar.Game < GAME_UE5_5)
        {
            var min = Ar.Read<TIntVector2<ushort>>();
            Min = new FIntVector2(min.X, min.Y);
            var size = Ar.Read<TIntVector2<ushort>>();
            Size = new FIntVector2(size.X, size.Y);
            if (Ar.Game is GAME_Gothic1Remake) Ar.Position += 4;
            Id = Ar.Read<uint>();
        }
        else
        {
            Min = Ar.Read<FIntVector2>();
            Size = Ar.Read<FIntVector2>();
            Id = Ar.Read<ulong>();
        }

        Priority = Ar.Read<int>();

        if (version >= 6)
            Packed = Ar.Game <= GAME_UE5_5 ? Ar.Read<ushort>() : Ar.Read<uint>();
        else if (version == 5)
            Packed = Ar.Read<byte>();
    }
}

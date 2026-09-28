using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Rendering;
using CUE4Parse.UE4.Readers;
using FLocation64 = CUE4Parse.UE4.Objects.Core.Rendering.TLocation<long>;

namespace CUE4Parse.UE4.Objects.Engine.InstanceData;

public struct FCompressedSpatialHashItem
{
    public FLocation64 Location;
    public int NumInstances;
    public FRenderBounds ExplicitBounds;
    public FVector2D MinMaxInstanceScale;

    public FCompressedSpatialHashItem(FArchive Ar)
    {
        Location = Ar.Read<FLocation64>();
        NumInstances = Ar.Read<int>();
        if (Ar.Game >= GAME_UE5_8) ExplicitBounds = Ar.Read<FRenderBounds>();
        if (Ar.Game >= GAME_UE6_0) MinMaxInstanceScale = Ar.Read<FVector2D>();
    }
}

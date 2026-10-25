using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Core.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct FRenderBounds
{
    public FVector Min;
    public FVector Max;

    public FRenderBounds(FVector min, FVector max)
    {
        Min = min;
        Max = max;
    }

    public FRenderBounds(FBox box)
    {
        Min = box.Min;
        Max = box.Max;
    }

    public FRenderBounds(FBoxSphereBounds bounds)
    {
        Min = bounds.Origin - bounds.BoxExtent;
        Max = bounds.Origin + bounds.BoxExtent;
    }

    public FBox ToBox() => new(Min, Max);
    public FBoxSphereBounds ToBoxSphereBounds() => new FBoxSphereBounds(ToBox());
}

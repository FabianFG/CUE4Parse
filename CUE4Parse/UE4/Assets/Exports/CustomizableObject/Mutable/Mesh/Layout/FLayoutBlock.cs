using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

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
}
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

[StructLayout(LayoutKind.Sequential)]
public struct FShape
{
    public FVector Position;
    public FVector Up;
    public FVector Side;
    public FVector Size;
    public EType Type;
}

public enum EType : byte
{
    None = 0,
    Ellipse,
    AABox
}
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FClothingStreamable
{
    public readonly int ClothingAssetIndex;
    public readonly int ClothingAssetLod;
    public readonly int PhysicsAssetIndex;
    public readonly uint Size;
    public readonly FMutableStreamableBlock Block;
    public readonly uint SourceId;
}
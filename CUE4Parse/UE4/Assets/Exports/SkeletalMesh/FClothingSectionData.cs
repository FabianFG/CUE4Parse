using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[StructLayout(LayoutKind.Sequential)]
public struct FClothingSectionData
{
    public readonly FGuid AssetGuid;
    public readonly int AssetLodIndex;
}

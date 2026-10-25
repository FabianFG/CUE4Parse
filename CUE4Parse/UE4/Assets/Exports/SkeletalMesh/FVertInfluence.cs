using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
public struct FVertInfluence
{
    public float Weight;
    public uint VertIndex;
    public ushort BoneIndex;
}

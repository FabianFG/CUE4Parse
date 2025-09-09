using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public class FVPTree(FAssetArchive Ar)
{
    public FVPTreeNode[] Nodes = Ar.ReadArray<FVPTreeNode>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FVPTreeNode
{
    public int Index;
    public float Distance;
    public int LeftIndex;
    public int RightIndex;
}

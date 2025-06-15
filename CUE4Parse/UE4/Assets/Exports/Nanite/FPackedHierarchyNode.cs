using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FPackedHierarchyNode
{
    public FHierarchyNodeSlice[] Slices;

    public FPackedHierarchyNode(FArchive Ar, uint index)
    {
        Slices = new FHierarchyNodeSlice[NANITE_MAX_BVH_NODE_FANOUT];
        for (uint i = 0; i < NANITE_MAX_BVH_NODE_FANOUT; i++)
        {
            Slices[i] = new FHierarchyNodeSlice(Ar, index, i);
        }
    }
}

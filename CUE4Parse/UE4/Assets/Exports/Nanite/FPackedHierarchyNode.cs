using CUE4Parse.UE4.Readers;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FPackedHierarchyNode(FArchive Ar)
{
    public FHierarchyNodeSlice[] Slices = Ar.ReadArray(NANITE_MAX_BVH_NODE_FANOUT, () => new FHierarchyNodeSlice(Ar));
}

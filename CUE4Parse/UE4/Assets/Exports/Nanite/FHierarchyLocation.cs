namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public struct FHierarchyLocation
{
    public uint ChildIndex_NodeIndex;
    
    public uint ChildIndex => ChildIndex_NodeIndex & NaniteConstants.NANITE_MAX_BVH_NODE_FANOUT_MASK;
    public uint NodeIndex => ChildIndex_NodeIndex >> NaniteConstants.NANITE_MAX_BVH_NODE_FANOUT_BITS;
}
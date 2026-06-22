namespace CUE4Parse.UE4.Assets.Exports.Chaos.GeometryCollection;

public struct FGeometryCollectionBoneNode
{
    enum ENodeFlags : uint
    {
        // A node is currently either a geometry node (bit set) or a null node with a transform only (bit zero)
        FS_Geometry = 0x00000001,

        // additional flags
        FS_Clustered = 0x00000002,

        // Gets deleted from world instead of becoming a fractured chunk in the world
        FS_RemoveOnFracture = 0x00000004
    }

    /** Level in Hierarchy : 0 is usually but not necessarily always the root */
    public int Level;

    /** Parent bone index : use InvalidBone for root parent */
    public int Parent;

    /** Child bone indices */
    public int[] Children;

    /** Flags to store any state for each node */
    public uint StatusFlags;
}
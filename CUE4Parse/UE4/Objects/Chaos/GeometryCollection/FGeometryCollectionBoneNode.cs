namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FGeometryCollectionBoneNode
{
    public readonly int[] Children;
    public readonly int Level;
    public readonly int Parent;
    public readonly ENodeFlags StatusFlags;

    public FGeometryCollectionBoneNode(FChaosArchive Ar)
    {
        Level = Ar.Read<int>();
        Parent = Ar.Read<int>();
        Children = Ar.ReadArray<int>();
        StatusFlags = Ar.Read<ENodeFlags>();
    }
}

[Flags]
public enum ENodeFlags : uint
{
    // A node is currently either a geometry node (bit set) or a null node with a transform only (bit zero)
    FS_Geometry = 0x00000001,

    // additional flags
    FS_Clustered = 0x00000002,

    // Gets deleted from world instead of becoming a fractured chunk in the world
    FS_RemoveOnFracture = 0x00000004
}
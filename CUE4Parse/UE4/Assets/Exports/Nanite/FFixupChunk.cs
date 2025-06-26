using System.IO;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FFixupChunk
{
    public struct FHeader
    {
        public readonly ushort NumClusters;
        public readonly ushort NumHierachyFixups;
        public readonly ushort NumClusterFixups;

        public FHeader(FArchive Ar)
        {
            // the NF header was add in 5.3 in previous versions it isn't there
            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                ushort magic = Ar.Read<ushort>();
                if (magic != NANITE_FIXUP_MAGIC) //NF
                {
                    throw new InvalidDataException($"Invalid magic value, expected {NANITE_FIXUP_MAGIC:04x} got {magic:04x}");
                }
            }
            NumClusters = Ar.Read<ushort>();
            NumHierachyFixups = Ar.Read<ushort>();
            NumClusterFixups = Ar.Read<ushort>();
            if (Ar.Game < EGame.GAME_UE5_3) Ar.Position += 2;
        }
    }

    public readonly FHeader Header;
    public FHierarchyFixup[] HierarchyFixups;
    public FClusterFixup[] ClusterFixups;

    public FFixupChunk(FArchive Ar)
    {
        Header = new FHeader(Ar);
        HierarchyFixups = Ar.ReadArray(Header.NumHierachyFixups, () => new FHierarchyFixup(Ar));
        ClusterFixups = Ar.ReadArray(Header.NumClusterFixups, () => new FClusterFixup(Ar));
    }
}

public class FHierarchyFixup
{
    public uint PageIndex;
    public uint NodeIndex;
    public uint ChildIndex;
    public uint ClusterGroupPartStartIndex;
    public uint PageDependencyStart;
    public uint PageDependencyNum;

    public FHierarchyFixup(FArchive Ar)
    {
        PageIndex = Ar.Read<uint>();

        var hierarchyNodeAndChildIndex = Ar.Read<uint>();
        NodeIndex = hierarchyNodeAndChildIndex >> NANITE_MAX_HIERACHY_CHILDREN_BITS;
        ChildIndex = hierarchyNodeAndChildIndex & (NANITE_MAX_HIERACHY_CHILDREN - 1);

        ClusterGroupPartStartIndex = Ar.Read<uint>();

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> NANITE_MAX_GROUP_PARTS_BITS;
        PageDependencyNum = pageDependencyStartAndNum & NANITE_MAX_GROUP_PARTS_MASK;
    }
}

public class FClusterFixup
{
    public uint PageIndex;
    public uint ClusterIndex;
    public uint PageDependencyStart;
    public uint PageDependencyNum;

    public FClusterFixup(FArchive Ar)
    {
        var pageAndClusterIndex = Ar.Read<uint>();
        PageIndex = pageAndClusterIndex >> NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE_BITS(Ar.Game);
        ClusterIndex = pageAndClusterIndex & (uint) (NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE(Ar.Game) - 1);

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> NANITE_MAX_GROUP_PARTS_BITS;
        PageDependencyNum = pageDependencyStartAndNum & NANITE_MAX_GROUP_PARTS_MASK;
    }
}

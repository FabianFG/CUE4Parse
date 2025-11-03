using System.IO;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FFixupChunk
{
    public struct FHeader
    {
        public readonly ushort NumClusters;
        public readonly ushort NumGroupFixups;
        public readonly ushort NumPartFixups;
        public readonly ushort NumHierarchyFixups;
        public readonly ushort NumClusterFixups;
        public readonly uint NumParentFixups;
        public readonly uint NumHierarchyLocations;
        public readonly uint NumClusterIndices;
        
        /// <summary>
        /// Pages that need to be reconsidered for fixup when this page is installed/uninstalled. The last pages of any groups in the page.
        /// </summary>
        public readonly ushort NumReconsiderPages;

        public FHeader(FArchive Ar)
        {
            // the NF header was added in 5.3 in previous versions it isn't there
            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                var magic = Ar.Read<ushort>();
                if (magic != NANITE_FIXUP_MAGIC) //NF
                    throw new InvalidDataException($"Invalid magic value, expected {NANITE_FIXUP_MAGIC:04x} got {magic:04x}");
            }

            if (Ar.Game >= EGame.GAME_UE5_7)
            {
                NumGroupFixups = Ar.Read<ushort>();
                NumPartFixups = Ar.Read<ushort>();
            }
            
            NumClusters = Ar.Read<ushort>();

            if (Ar.Game >= EGame.GAME_UE5_7)
            {
                NumReconsiderPages = Ar.Read<ushort>();
                Ar.Position += sizeof(ushort); // pad
                NumParentFixups = Ar.Read<uint>();
                NumHierarchyLocations = Ar.Read<uint>();
                NumClusterIndices = Ar.Read<uint>();
            }
            else
            {
                NumHierarchyFixups = Ar.Read<ushort>();
                NumClusterFixups = Ar.Read<ushort>();
            }
            
            if (Ar.Game < EGame.GAME_UE5_3) Ar.Position += 2;
        }
    }

    public readonly FHeader Header;
    public FGroupFixup[] GroupFixups;
    public FPartFixup[] PartFixups;
    public FParentFixup[] ParentFixups;
    public FHierarchyLocation[] HierarchyLocations;
    public ushort[] ReconsiderPageIndexes;
    public byte[] ClusterIndex;
    
    public FHierarchyFixup[] HierarchyFixups;
    public FClusterFixup[] ClusterFixups;

    public FFixupChunk(FArchive Ar)
    {
        Header = new FHeader(Ar);

        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            GroupFixups = Ar.ReadArray<FGroupFixup>(Header.NumGroupFixups);
            PartFixups = Ar.ReadArray<FPartFixup>(Header.NumPartFixups);
            ParentFixups = Ar.ReadArray<FParentFixup>((int)Header.NumParentFixups);
            HierarchyLocations = Ar.ReadArray<FHierarchyLocation>((int)Header.NumHierarchyLocations);
            ReconsiderPageIndexes = Ar.ReadArray<ushort>(Header.NumReconsiderPages);
            ClusterIndex = Ar.ReadArray<byte>((int)Header.NumClusterIndices);
        }
        else
        {
            HierarchyFixups = Ar.ReadArray(Header.NumHierarchyFixups, () => new FHierarchyFixup(Ar));
            ClusterFixups = Ar.ReadArray(Header.NumClusterFixups, () => new FClusterFixup(Ar));
        }
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
        PageDependencyStart = pageDependencyStartAndNum >> NANITE_MAX_GROUP_PARTS_BITS(Ar.Game);
        PageDependencyNum = pageDependencyStartAndNum & NANITE_MAX_GROUP_PARTS_MASK(Ar.Game);
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
        PageIndex = pageAndClusterIndex >> NANITE_MAX_CLUSTERS_PER_PAGE_BITS(Ar.Game);
        ClusterIndex = pageAndClusterIndex & (uint) (NANITE_MAX_CLUSTERS_PER_PAGE(Ar.Game) - 1);

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> NANITE_MAX_GROUP_PARTS_BITS(Ar.Game);
        PageDependencyNum = pageDependencyStartAndNum & NANITE_MAX_GROUP_PARTS_MASK(Ar.Game);
    }
}

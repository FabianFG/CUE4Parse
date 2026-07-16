using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FHierarchyNodeSlice
{
    public FVector4 LODBounds;
    public FVector BoxBoundsCenter;
    public FVector BoxBoundsExtent;
    public float MinLODError;
    public float MaxParentLODError;
    public uint ChildStartReference;    // Can be node (index) or cluster (page:cluster)
    public uint NumChildren;
    public uint StartPageIndex;
    public uint NumPages;
    public uint AssemblyTransformIndex;
    public uint ResourcePageRangeKey;
    public uint	NumBones;
    public uint	BoneIndicesOffsetInDwords;
    public bool bEnabled;
    public bool bLoaded;
    public bool bLeaf;

    public FHierarchyNodeSlice(FArchive Ar)
    {
        LODBounds = Ar.Read<FVector4>();
        BoxBoundsCenter = Ar.Read<FVector>();
        MinLODError = (float) Ar.Read<Half>();
        MaxParentLODError = (float) Ar.Read<Half>();
        BoxBoundsExtent = Ar.Read<FVector>();
        ChildStartReference = Ar.Read<uint>();
        bLoaded = ChildStartReference != 0xFFFFFFFFu;
        if (Ar.Game >= GAME_UE5_7)
        {
            uint x;
            uint y;

            if (Ar.Game >= GAME_UE5_8)
            {
                var misc2 = Ar.Read<TIntVector3<uint>>();
                x = misc2.X;
                y = misc2.Y;

                BoneIndicesOffsetInDwords = GetBits(misc2.Z, NANITE_HIERARCHY_BONE_INDICES_OFFSET_BITS, 0);
                NumBones = GetBits(misc2.Z, NANITE_HIERARCHY_NUM_BONES_BITS, NANITE_HIERARCHY_BONE_INDICES_OFFSET_BITS);
            }
            else
            {
                var misc2 = Ar.Read<TIntVector2<uint>>();
                x = misc2.X;
                y = misc2.Y;
            }

            AssemblyTransformIndex = GetBits(y, NANITE_HIERARCHY_ASSEMBLY_TRANSFORM_INDEX_BITS, 0);
            NumChildren = GetBits(y, NANITE_MAX_CLUSTERS_PER_GROUP_BITS, NANITE_HIERARCHY_ASSEMBLY_TRANSFORM_INDEX_BITS);

            bLeaf = x != 0xFFFFFFFFu;
            if (bLeaf)
            {
                ResourcePageRangeKey = x;
                bEnabled = ResourcePageRangeKey != NANITE_PAGE_RANGE_KEY_EMPTY_RANGE || NumChildren > 0;
            }
            else
            {
                ResourcePageRangeKey = NANITE_PAGE_RANGE_KEY_EMPTY_RANGE;
                bEnabled = true;
            }
        }
        else
        {
            var misc2 = Ar.Read<uint>();
            NumChildren = GetBits(misc2, NANITE_MAX_CLUSTERS_PER_GROUP_BITS, 0);
            NumPages = GetBits(misc2, NANITE_MAX_GROUP_PARTS_BITS(Ar.Game), NANITE_MAX_CLUSTERS_PER_GROUP_BITS);
            StartPageIndex = GetBits(misc2, NANITE_MAX_RESOURCE_PAGES_BITS(Ar.Game), NANITE_MAX_CLUSTERS_PER_GROUP_BITS + NANITE_MAX_GROUP_PARTS_BITS(Ar.Game));
            bEnabled = misc2 != 0u;
            bLeaf = misc2 != 0xFFFFFFFFu;
            AssemblyTransformIndex = 0xFFFFFFFFu;
            BoneIndicesOffsetInDwords = 0xFFFFFFFFu;
            NumBones = 0xFFFFFFFFu;
        }
    }
}

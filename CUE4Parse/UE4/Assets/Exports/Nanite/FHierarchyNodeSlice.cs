using System;
using System.Runtime.CompilerServices;
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
    public bool bEnabled;
    public bool bLoaded;
    public bool bLeaf;
    public uint NodeIndex;
    public uint SliceIndex;

    public FHierarchyNodeSlice(FArchive Ar, uint index, uint sliceIndex)
    {
        NodeIndex = index;
        SliceIndex = sliceIndex;
        
        LODBounds = Ar.Read<FVector4>();
        BoxBoundsCenter = Ar.Read<FVector>();
        MinLODError = (float) Ar.Read<Half>();
        MaxParentLODError = (float) Ar.Read<Half>();
        BoxBoundsExtent = Ar.Read<FVector>();
        ChildStartReference = Ar.Read<uint>();
        bLoaded = ChildStartReference != 0xFFFFFFFFu;

        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            var resourcePageRangeKey = Ar.Read<uint>();
            var groupPartSize_AssemblyPartIndex = Ar.Read<uint>();
        }
        else
        {
            var misc2 = Ar.Read<uint>();
            NumChildren = GetBits(misc2, NANITE_MAX_CLUSTERS_PER_GROUP_BITS, 0);
            NumPages = GetBits(misc2, NANITE_MAX_GROUP_PARTS_BITS, NANITE_MAX_CLUSTERS_PER_GROUP_BITS);
            StartPageIndex = GetBits(misc2, NANITE_MAX_RESOURCE_PAGES_BITS, NANITE_MAX_CLUSTERS_PER_GROUP_BITS + NANITE_MAX_GROUP_PARTS_BITS);
            bEnabled = misc2 != 0u;
            bLeaf = misc2 != 0xFFFFFFFFu;
        }
        // #if NANITE_ASSEMBLY_DATA 5.6+ but set to 0
        //         Ar << Node.Misc2[ i ].AssemblyPartIndex;
        // #endif
    }
}

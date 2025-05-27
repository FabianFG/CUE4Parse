using System;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class FPackedHierarchyNode
{
    private const int NANITE_MAX_BVH_NODE_FANOUT_BITS = 2;
    private const int NANITE_MAX_BVH_NODE_FANOUT = 1 << NANITE_MAX_BVH_NODE_FANOUT_BITS;

    public FVector4[] LODBounds;
    public FMisc0[] Misc0;
    public FMisc1[] Misc1;
    public FMisc2[] Misc2;

    public FPackedHierarchyNode(FArchive Ar)
    {
        LODBounds = new FVector4[NANITE_MAX_BVH_NODE_FANOUT];
        Misc0 = new FMisc0[NANITE_MAX_BVH_NODE_FANOUT];
        Misc1 = new FMisc1[NANITE_MAX_BVH_NODE_FANOUT];
        Misc2 = new FMisc2[NANITE_MAX_BVH_NODE_FANOUT];

        for (var i = 0; i < NANITE_MAX_BVH_NODE_FANOUT; i++)
        {
            LODBounds[i] = Ar.Read<FVector4>();
            Misc0[i] = new FMisc0(Ar);
            Misc1[i] = Ar.Read<FMisc1>();
            Misc2[i] = new FMisc2(Ar);
        }
    }

    public class FMisc0
    {
        public FVector BoxBoundsCenter;
        public float MinLODError;
        public float MaxParentLODError;

        public FMisc0(FArchive Ar)
        {
            BoxBoundsCenter = Ar.Read<FVector>();

            var minLODError_maxParentLODError = Ar.Read<uint>();
            MinLODError = minLODError_maxParentLODError;
            MaxParentLODError = minLODError_maxParentLODError >> 16;
        }
    }

    public struct FMisc1
    {
        public FVector BoxBoundsExtent;
        public uint ChildStartReference;
        public bool bLoaded => ChildStartReference != 0xFFFFFFFFu;
    }

    public class FMisc2
    {
        private const int NANITE_MAX_CLUSTERS_PER_GROUP_BITS = 9;
        private const int NANITE_MAX_RESOURCE_PAGES_BITS = 20;

        public uint NumChildren;
        public uint NumPages;
        public uint StartPageIndex;
        public bool bEnabled;
        public bool bLeaf;

        public FMisc2(FArchive Ar)
        {
            var resourcePageIndex_numPages_groupPartSize = Ar.Read<uint>();
            NumChildren = FCluster.GetBits(resourcePageIndex_numPages_groupPartSize, NANITE_MAX_CLUSTERS_PER_GROUP_BITS, 0);
            NumPages = FCluster.GetBits(resourcePageIndex_numPages_groupPartSize, FHierarchyFixup.NANITE_MAX_GROUP_PARTS_BITS, NANITE_MAX_CLUSTERS_PER_GROUP_BITS);
            StartPageIndex = FCluster.GetBits(resourcePageIndex_numPages_groupPartSize, NANITE_MAX_RESOURCE_PAGES_BITS, NANITE_MAX_CLUSTERS_PER_GROUP_BITS + FHierarchyFixup.NANITE_MAX_GROUP_PARTS_BITS);
            bEnabled = resourcePageIndex_numPages_groupPartSize != 0u;
            bLeaf = resourcePageIndex_numPages_groupPartSize != 0xFFFFFFFFu;
        }
    }
}

public class FPageStreamingState
{
    public uint BulkOffset;
    public uint BulkSize;
    public uint PageSize;
    public uint DependenciesStart;
    public uint DependenciesNum;
    public byte MaxHierarchyDepth;
    public uint Flags;

    public FPageStreamingState(FAssetArchive Ar)
    {
        BulkOffset = Ar.Read<uint>();
        BulkSize = Ar.Read<uint>();
        PageSize = Ar.Read<uint>();
        DependenciesStart = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            DependenciesNum = Ar.Read<ushort>();
            MaxHierarchyDepth = Ar.Read<byte>();
            Flags = Ar.Read<byte>();
        }
        else
        {
            DependenciesNum = Ar.Read<uint>();
            // doesn't exist in 5.0EA
            Flags = Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES ? Ar.Read<uint>() : 0;
        }
    }
}

public class FFixupChunk
{
    public class FHeader
    {
        public readonly ushort NumClusters;
        public readonly ushort NumHierachyFixups;
        public readonly ushort NumClusterFixups;

        public FHeader(FArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                ushort magic = Ar.Read<ushort>();
                if (magic != 0x464Eu) //NF
                {
                    throw new InvalidDataException($"Invalid magic value, expected {0x464Eu:04x} got {magic:04x}");
                }
            }
            NumClusters = Ar.Read<ushort>();
            NumHierachyFixups = Ar.Read<ushort>();
            NumClusterFixups = Ar.Read<ushort>();
            if (Ar.Game < EGame.GAME_UE5_3)
            {
                // 2 byte padding
                Ar.Position += 2;
            }
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
    private const int NANITE_MAX_HIERACHY_CHILDREN_BITS = 6;
    public const int NANITE_MAX_GROUP_PARTS_BITS = 3;
    private const int NANITE_MAX_HIERACHY_CHILDREN = (1 << NANITE_MAX_HIERACHY_CHILDREN_BITS);
    public const int NANITE_MAX_GROUP_PARTS_MASK = ((1 << NANITE_MAX_GROUP_PARTS_BITS) - 1);

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
    private const int NANITE_MAX_CLUSTERS_PER_PAGE_BITS = 8;
    private const int NANITE_MAX_CLUSTERS_PER_PAGE = (1 << NANITE_MAX_CLUSTERS_PER_PAGE_BITS);

    public uint PageIndex;
    public uint ClusterIndex;
    public uint PageDependencyStart;
    public uint PageDependencyNum;

    public FClusterFixup(FArchive Ar)
    {
        var pageAndClusterIndex = Ar.Read<uint>();
        PageIndex = pageAndClusterIndex >> NANITE_MAX_CLUSTERS_PER_PAGE_BITS;
        ClusterIndex = pageAndClusterIndex & (NANITE_MAX_CLUSTERS_PER_PAGE - 1u);

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> FHierarchyFixup.NANITE_MAX_GROUP_PARTS_BITS;
        PageDependencyNum = pageDependencyStartAndNum & FHierarchyFixup.NANITE_MAX_GROUP_PARTS_MASK;
    }
}

public class FCluster
{
    private const int NANITE_MIN_POSITION_PRECISION = -8;

    public uint NumVerts;
    public uint PositionOffset;
    public uint NumTris;
    public uint IndexOffset;
    public uint ColorMin;
    public uint ColorBits;
    public uint GroupIndex;
    public FIntVector PosStart;
    public uint BitsPerIndex;
    public int PosPrecision;
    public uint PosBitsX;
    public uint PosBitsY;
    public uint PosBitsZ;
    public FVector4 LODBounds;
    public FVector BoxBoundsCenter;
    public float LODError;
    public float EdgeLength;
    public FVector BoxBoundsExtent;
    public uint Flags;
    public uint AttributeOffset;
    public uint BitsPerAttribute;
    public uint DecodeInfoOffset;
    public uint NumUVs;
    public uint ColorMode;
    public uint UV_Prec;
    public uint MaterialTableOffset;
    public uint MaterialTableLength;
    public uint Material0Index;
    public uint Material1Index;
    public uint Material2Index;
    public uint Material0Length;
    public uint Material1Length;
    public uint VertReuseBatchCountTableOffset;
    public uint VertReuseBatchCountTableSize;
    public TIntVector4<uint> VertReuseBatchInfo;



    public FCluster(FArchive Ar) : this(Ar, 1)
    {
    }

    public FCluster(FArchive Ar, int numClusters) {
        if (numClusters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numClusters), $"{nameof(numClusters)} should never be 0 or a negative value.");
        }
        // clusters are stored in SOA layout so we gotta walk the stride.
        int stride = 16 * (numClusters - 1);

        var numVerts_positionOffset = Ar.Read<uint>();
        NumVerts = GetBits(numVerts_positionOffset, 9, 0);
        PositionOffset = GetBits(numVerts_positionOffset, 23, 9);

        var numTris_indexOffset = Ar.Read<uint>();
        NumTris = GetBits(numTris_indexOffset, 8, 0);
        IndexOffset = GetBits(numTris_indexOffset, 24, 8);

        ColorMin = Ar.Read<uint>();

        var colorBits_groupIndex = Ar.Read<uint>();
        ColorBits = GetBits(colorBits_groupIndex, 16, 0);
        GroupIndex = GetBits(colorBits_groupIndex, 16, 16); // debug only

        Ar.Position += stride;
        PosStart = Ar.Read<FIntVector>();

        var bitsPerIndex_posPrecision_posBits = Ar.Read<uint>();
        BitsPerIndex = GetBits(bitsPerIndex_posPrecision_posBits, 4, 0);
        PosPrecision = (int)GetBits(bitsPerIndex_posPrecision_posBits, 5, 4) + NANITE_MIN_POSITION_PRECISION;
        PosBitsX = GetBits(bitsPerIndex_posPrecision_posBits, 5, 9);
        PosBitsY = GetBits(bitsPerIndex_posPrecision_posBits, 5, 14);
        PosBitsZ = GetBits(bitsPerIndex_posPrecision_posBits, 5, 19);

        Ar.Position += stride;
        LODBounds = Ar.Read<FVector4>();

        Ar.Position += stride;
        BoxBoundsCenter = Ar.Read<FVector>();
        var lODError_edgeLength = Ar.Read<uint>();
        LODError = lODError_edgeLength;
        EdgeLength = lODError_edgeLength >> 16;

        Ar.Position += stride;
        BoxBoundsExtent = Ar.Read<FVector>();
        Flags = Ar.Read<uint>();

        Ar.Position += stride;
        var attributeOffset_bitsPerAttribute = Ar.Read<uint>();
        AttributeOffset = GetBits(attributeOffset_bitsPerAttribute, 22, 0);
        BitsPerAttribute = GetBits(attributeOffset_bitsPerAttribute, 10, 22);

        var decodeInfoOffset_numUVs_colorMode = Ar.Read<uint>();
        DecodeInfoOffset = GetBits(decodeInfoOffset_numUVs_colorMode, 22, 0);
        NumUVs = GetBits(decodeInfoOffset_numUVs_colorMode, 3, 22);
        ColorMode = GetBits(decodeInfoOffset_numUVs_colorMode, 2, 22 + 3);

        UV_Prec = Ar.Read<uint>();

        Ar.Position += stride;
        var materialEncoding = Ar.Read<uint>();
        if (materialEncoding < 0xFE000000u)
        {
            MaterialTableOffset = 0;
            MaterialTableLength	= 0;
            Material0Index = GetBits(materialEncoding, 6, 0);
            Material1Index = GetBits(materialEncoding, 6, 6);
            Material2Index = GetBits(materialEncoding, 6, 12);
            Material0Length = GetBits(materialEncoding, 7, 18) + 1;
            Material1Length = GetBits(materialEncoding, 7, 25);
            VertReuseBatchCountTableOffset = 0;
            VertReuseBatchCountTableSize = 0;

            Ar.Position += stride;
            VertReuseBatchInfo = Ar.Read<TIntVector4<uint>>();
        }
        else
        {
            MaterialTableOffset = GetBits(materialEncoding, 19, 0);
            MaterialTableLength = GetBits(materialEncoding, 6, 19) + 1;
            Material0Index = 0;
            Material1Index = 0;
            Material2Index = 0;
            Material0Length = 0;
            Material1Length = 0;
            VertReuseBatchCountTableOffset = Ar.Read<uint>();
            VertReuseBatchCountTableSize = Ar.Read<uint>();

            Ar.Position += stride;
            VertReuseBatchInfo = default;
        }
    }



    public static uint GetBits(uint value, int numBits, int offset)
    {
        uint mask = (1u << numBits) - 1u;
        return (value >> offset) & mask;
    }
}

public readonly struct FRootPageInfo
{
    public readonly uint RuntimeResourceID;
    public readonly uint NumClusters;
}

public readonly struct FPageDiskHeader
{
    public readonly uint GpuSize;
    public readonly uint NumClusters;
    public readonly uint NumRawFloat4s;
    public readonly uint NumTexCoords;
    public readonly uint NumVertexRefs;
    public readonly uint DecodeInfoOffset;
    public readonly uint StripBitmaskOffset;
    public readonly uint VertexRefBitmaskOffset;
}

public readonly struct FClusterDiskHeader
{
    public readonly uint IndexDataOffset;
    public readonly uint PageClusterMapOffset;
    public readonly uint VertexRefDataOffset;
    public readonly uint PositionDataOffset;
    public readonly uint AttributeDataOffset;
    public readonly uint NumVertexRefs;
    public readonly uint NumPrevRefVerticesBeforeDwords;
    public readonly uint NumPrevNewVerticesBeforeDwords;
}

public readonly struct FPageGPUHeader
{
    public readonly uint NumClusters;
    [JsonIgnore]
    public readonly uint Pad1;
    [JsonIgnore]
    public readonly uint Pad2;
    [JsonIgnore]
    public readonly uint Pad3;
}

public class FNaniteStreamableData
{
    public FFixupChunk FixupChunk;

    public FRootPageInfo[]? RootPageInfos;
    public FCluster[] Clusters;
    public FPageDiskHeader? PageDiskHeader;
    public FClusterDiskHeader[]? ClusterDiskHeaders;
    public FPageGPUHeader? PageGPUHeader;

    public unsafe FNaniteStreamableData(FArchive Ar, int numRootPages, uint pageSize)
    {
        FixupChunk = new FFixupChunk(Ar);

        // todo: check how it's actually serialized in pre 5.3
        if (Ar.Game < EGame.GAME_UE5_3)
        {
            RootPageInfos = Ar.ReadArray<FRootPageInfo>(numRootPages);
            Clusters = Ar.ReadArray(0, () => new FCluster(Ar));
            Ar.Position += pageSize - sizeof(FRootPageInfo) * numRootPages;
            PageDiskHeader = null;
            ClusterDiskHeaders = null;
            PageGPUHeader = null;
        }
        else
        {
            RootPageInfos = null;

            // origin of all the offsets in the page cluster header
            long pageDataOrigin = Ar.Position;
            PageDiskHeader = Ar.Read<FPageDiskHeader>();
            if (PageDiskHeader.Value.NumClusters > 0xFF)
            {
                throw new InvalidDataException($"Too many clusters in FNaniteStreamableData, {PageDiskHeader.Value.NumClusters} max is 256");
            }
            if (PageDiskHeader.Value.NumTexCoords > 4)
            {
                throw new InvalidDataException($"Too many tex coords in FNaniteStreamableData, {PageDiskHeader.Value.NumTexCoords} max is 4");
            }

            ClusterDiskHeaders = Ar.ReadArray<FClusterDiskHeader>((int) PageDiskHeader.Value.NumClusters);

            PageGPUHeader = Ar.Read<FPageGPUHeader>();
            if (PageDiskHeader.Value.NumClusters != PageDiskHeader.Value.NumClusters)
            {
                throw new InvalidDataException($"Too many tex coords in FNaniteStreamableData, {PageDiskHeader.Value.NumTexCoords} max is 4");
            }

            // Not stored as an array, it's actually stored as an SOA to speedup GPU transcoding
            Clusters = new FCluster[PageGPUHeader.Value.NumClusters];
            long clusterOrigin = Ar.Position;
            for (int i = 0; i < Clusters.Length; i++)
            {
                Ar.Position = clusterOrigin + 16 * i;
                Clusters[i] = new FCluster(Ar, Clusters.Length);
            }
            Ar.Position = clusterOrigin + (16 * 7 * Clusters.Length);
            // material table
            if (Clusters.Any(c => c.MaterialTableLength > 0))
            {
                // not figured out yet
            }
            // figure out
            // decode info
            // tri indexes
            // page-cluster map
            // ref vertex data
            // non-ref vertex position data
            // attribute data
        }
    }
}

public class FNaniteResources
{
    // Persistent State
    public FNaniteStreamableData RootData; // Root page is loaded on resource load, so we always have something to draw.
    public FByteBulkData StreamablePages; // Remaining pages are streamed on demand.
    public ushort[] ImposterAtlas;
    public FPackedHierarchyNode[] HierarchyNodes;
    public uint[] HierarchyRootOffsets;
    public FPageStreamingState[] PageStreamingStates;
    public uint[] PageDependencies;
    public FMatrix3x4[] AssemblyTransforms;
    public FBoxSphereBounds MeshBounds; // FBoxSphereBounds3f
    public int NumRootPages = 0;
    public int PositionPrecision = 0;
    public int NormalPrecision = 0;
    public int TangentPrecision = 0;
    public uint NumInputTriangles = 0;
    public uint NumInputVertices = 0;
    public ushort NumInputMeshes = 0;
    public ushort NumInputTexCoords = 0;
    public uint NumClusters = 0;
    public uint ResourceFlags = 0;

    public FNaniteResources(FAssetArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped())
        {
            ResourceFlags = Ar.Read<uint>();
            StreamablePages = new FByteBulkData(Ar);

            var nanite = new FByteArchive("PackedCluster", Ar.ReadArray<byte>(), Ar.Versions);

            PageStreamingStates = Ar.ReadArray(() => new FPageStreamingState(Ar));
            HierarchyNodes = Ar.ReadArray(() => new FPackedHierarchyNode(Ar));
            HierarchyRootOffsets = Ar.ReadArray<uint>();
            PageDependencies = Ar.ReadArray<uint>();
            if (Ar.Game >= EGame.GAME_UE5_6)
            {
                AssemblyTransforms = Ar.ReadArray<FMatrix3x4>();
                MeshBounds = new FBoxSphereBounds(Ar.Read<FVector>(), Ar.Read<FVector>(), Ar.Read<float>());
            }
            ImposterAtlas = Ar.ReadArray<ushort>();
            NumRootPages = Ar.Read<int>();
            PositionPrecision = Ar.Read<int>();
            if (Ar.Game >= EGame.GAME_UE5_2) NormalPrecision = Ar.Read<int>();
            NumInputTriangles = Ar.Read<uint>();
            NumInputVertices = Ar.Read<uint>();
            if (Ar.Game < EGame.GAME_UE5_6)
            {
                NumInputMeshes = Ar.Read<ushort>();
                NumInputTexCoords = Ar.Read<ushort>();
            }
            if (Ar.Game >= EGame.GAME_UE5_1) NumClusters = Ar.Read<uint>();

            if (PageStreamingStates.Length > 0)
            {
                nanite.Position = PageStreamingStates[0].BulkOffset;
                RootData = new FNaniteStreamableData(nanite, NumRootPages, PageStreamingStates[0].PageSize);
            }
        }
    }
}

using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FPackedHierarchyNode
    {
        public const int MAX_BVH_NODE_FANOUT_BITS = 3;
        public const int MAX_BVH_NODE_FANOUT = 1 << MAX_BVH_NODE_FANOUT_BITS;

        public FVector4[] LODBounds;
        public FMisc0[] Misc0;
        public FMisc1[] Misc1;
        public FMisc2[] Misc2;

        public FPackedHierarchyNode(FArchive Ar)
        {
            LODBounds = Ar.ReadArray<FVector4>(MAX_BVH_NODE_FANOUT);
            Misc0 = Ar.ReadArray<FMisc0>(MAX_BVH_NODE_FANOUT);
            Misc1 = Ar.ReadArray<FMisc1>(MAX_BVH_NODE_FANOUT);
            Misc2 = Ar.ReadArray<FMisc2>(MAX_BVH_NODE_FANOUT);
        }

        public struct FMisc0
        {
            public FVector BoxBoundsCenter;
            public uint MinLODError_MaxParentLODError;
        }

        public struct FMisc1
        {
            public FVector BoxBoundsExtent;
            public uint ChildStartReference;
        }

        public struct FMisc2
        {
            public uint ResourcePageIndex_NumPages_GroupPartSize;
        }
    }

    public struct FPageStreamingState
    {
        public uint BulkOffset;
        public uint BulkSize;
        public uint PageSize;
        public uint DependenciesStart;
        public uint DependenciesNum;
        public uint Flags;
    }

    public class FNaniteResources
    {
        // Persistent State
        public byte[] RootData; // Root page is loaded on resource load, so we always have something to draw.
        public FByteBulkData StreamableClusterPages; // Remaining pages are streamed on demand.
        public ushort[] ImposterAtlas;
        public FPackedHierarchyNode[] HierarchyNodes;
        public uint[] HierarchyRootOffsets;
        public FPageStreamingState[] PageStreamingStates;
        public uint[] PageDependencies;
        public int NumRootPages = 0;
        public int PositionPrecision = 0;
        public uint NumInputTriangles = 0;
        public uint NumInputVertices = 0;
        public ushort NumInputMeshes = 0;
        public ushort NumInputTexCoords = 0;
        public uint ResourceFlags = 0;

        public FNaniteResources(FAssetArchive Ar)
        {
            var stripFlags = new FStripDataFlags(Ar);
            if (!stripFlags.IsDataStrippedForServer())
            {
                ResourceFlags = Ar.Read<uint>();
                StreamableClusterPages = new FByteBulkData(Ar);
                RootData = Ar.ReadArray<byte>();
                PageStreamingStates = Ar.ReadArray<FPageStreamingState>();

                HierarchyNodes = Ar.ReadArray(() => new FPackedHierarchyNode(Ar));
                HierarchyRootOffsets = Ar.ReadArray<uint>();
                PageDependencies = Ar.ReadArray<uint>();
                ImposterAtlas = Ar.ReadArray<ushort>();
                NumRootPages = Ar.Read<int>();
                PositionPrecision = Ar.Read<int>();
                NumInputTriangles = Ar.Read<uint>();
                NumInputVertices = Ar.Read<uint>();
                NumInputMeshes = Ar.Read<ushort>();
                NumInputTexCoords = Ar.Read<ushort>();
            }
        }
    }
}

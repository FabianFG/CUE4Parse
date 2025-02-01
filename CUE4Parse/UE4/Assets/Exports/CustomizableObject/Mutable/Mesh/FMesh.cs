using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Layout;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Surfaces;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

public class FMesh : IMutablePtr
{
    public FMeshBufferSet IndexBuffers;
    public FMeshBufferSet VertexBuffers;
    public KeyValuePair<EMeshBufferType, FMeshBufferSet>[] AdditionalBuffers;
    public FLayout[] Layouts;
    public uint[] SkeletonIDs;
    public FSkeleton Skeleton;
    public FPhysicsBody PhysicsBody;
    public EMeshFlags Flags;
    public FMeshSurface[] Surfaces;
    public string[] Tags;
    public ulong[] StreamedResources;
    public FBonePose[] BonePoses;
    public FBoneName[] BoneMap;
    public FPhysicsBody[] AdditionalPhysicsBodies;
    public uint MeshIDPrefix;
    public uint ReferenceID;

    public bool IsBroken { get; set; }

    public FMesh(FArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version == -1)
        {
            IsBroken = true;
            return;
        }

        IndexBuffers = new FMeshBufferSet(Ar);
        VertexBuffers = new FMeshBufferSet(Ar);
        AdditionalBuffers = Ar.ReadArray(() => new KeyValuePair<EMeshBufferType, FMeshBufferSet>(Ar.Read<EMeshBufferType>(), new FMeshBufferSet(Ar)));
        Layouts = Ar.ReadMutableArray(() => new FLayout(Ar));
        SkeletonIDs = Ar.ReadArray<uint>();
        Skeleton = new FSkeleton(Ar);
        PhysicsBody = new FPhysicsBody(Ar);
        Flags = Ar.Read<EMeshFlags>();
        Surfaces = Ar.ReadArray(() => new FMeshSurface(Ar));
        Tags = Ar.ReadArray(Ar.ReadMutableFString);
        StreamedResources = Ar.ReadArray<ulong>();
        BonePoses = Ar.ReadArray(() => new FBonePose(Ar));
        BoneMap = Ar.ReadArray(() => new FBoneName(Ar));
        AdditionalPhysicsBodies = Ar.ReadArray(() => new FPhysicsBody(Ar));
        MeshIDPrefix = Ar.Read<uint>();
        ReferenceID = Ar.Read<uint>();
    }
}

public enum EMeshBufferType
{
    None,
    SkeletonDeformBinding,
    PhysicsBodyDeformBinding,
    PhysicsBodyDeformSelection,
    PhysicsBodyDeformOffsets,
    MeshLaplacianData,
    MeshLaplacianOffsets,
    UniqueVertexMap
}

public enum EMeshFlags : uint
{
    None = 0,

    /** The mesh is formatted to be used for planar and cilyndrical projection */
    ProjectFormat = 1 << 0,

    /** The mesh is formatted to be used for wrapping projection */
    ProjectWrappingFormat = 1 << 1,

    /** The mesh is a reference to an external resource mesh. */
    IsResourceReference = 1 << 2,

    /** The mesh is a reference to an external resource mesh and must be loaded when first referenced. */
    IsResourceForceLoad = 1 << 3,
}

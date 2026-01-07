using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Buffers;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

public class FMesh
{
    public FMeshBufferSet IndexBuffers;
    public FMeshBufferSet VertexBuffers;
    public KeyValuePair<EMeshBufferType, FMeshBufferSet>[] AdditionalBuffers;
    public FLayout[] Layouts;
    public uint[] SkeletonIDs;
    public FSkeleton? Skeleton;
    public FPhysicsBody? PhysicsBody;
    public EMeshFlags Flags;
    public FMeshSurface[] Surfaces;
    public string[] Tags;
    public ulong[] StreamedResources;
    public FBonePose[] BonePoses;
    public FBoneName[] BoneMap;
    public FPhysicsBody[] AdditionalPhysicsBodies;
    public uint MeshIDPrefix;
    
    public FMesh(FMutableArchive Ar)
    {
        IndexBuffers = new FMeshBufferSet(Ar);
        VertexBuffers = new FMeshBufferSet(Ar);
        AdditionalBuffers = Ar.ReadArray(() => new KeyValuePair<EMeshBufferType, FMeshBufferSet>(Ar.Read<EMeshBufferType>(), new FMeshBufferSet(Ar)));
        Layouts = Ar.ReadPtrArray(() => new FLayout(Ar));
        SkeletonIDs = Ar.ReadArray<uint>();
        Skeleton = Ar.ReadPtr(() => new FSkeleton(Ar));
        PhysicsBody = Ar.ReadPtr(() => new FPhysicsBody(Ar));
        Flags = Ar.Read<EMeshFlags>();
        Surfaces = Ar.ReadArray(() => new FMeshSurface(Ar));
        Tags = Ar.ReadArray(Ar.ReadFString);
        StreamedResources = Ar.ReadArray<ulong>();
        BonePoses = Ar.ReadArray<FBonePose>();
        BoneMap = Ar.ReadArray<FBoneName>();
        AdditionalPhysicsBodies = Ar.ReadArray(() => new FPhysicsBody(Ar));
        MeshIDPrefix = Ar.Read<uint>();
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

[Flags]
public enum EMeshFlags : uint
{
    None = 0,

    /** The mesh is formatted to be used for planar and cilyndrical projection */
    ProjectFormat = 1 << 0,

    /** The mesh is formatted to be used for wrapping projection */
    ProjectWrappingFormat = 1 << 1,
}
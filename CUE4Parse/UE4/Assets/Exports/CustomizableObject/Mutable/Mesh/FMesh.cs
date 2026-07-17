using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Buffers;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

public class FMesh
{
    [JsonIgnore] public int Version = 23;
    public FMeshBufferSet IndexBuffers;
    public FMeshBufferSet VertexBuffers;
    public KeyValuePair<EMeshBufferType, FMeshBufferSet>[] AdditionalBuffers;
    public FLayout[] Layouts;
    public uint[] SkeletonIDs = [];
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
    public FCloth[] ClothSections;
    public FMeshMorph Morph;
    public uint[] MorphDataBuffer;
    public FSkinWeightProfile[] SkinWeightProfiles;

    public FMeshBufferSet? FaceBuffers;
    public FACE_GROUP_DEPRECATED[] FaceGroups = [];
    public FMeshSurfaceLegacy[] LegacyMeshSurfaces = [];
    public uint ReferenceID;

    public FMesh(FMutableArchive Ar)
    {
        if (Ar.Game < GAME_UE5_6) Version = Ar.Read<int>();

        IndexBuffers = new FMeshBufferSet(Ar);
        VertexBuffers = new FMeshBufferSet(Ar);
        if (Version <= 18)
        {
            FaceBuffers = new FMeshBufferSet(Ar);
        }
        AdditionalBuffers = Ar.ReadArray(() => new KeyValuePair<EMeshBufferType, FMeshBufferSet>(Ar.Read<EMeshBufferType>(), new FMeshBufferSet(Ar)));
        Layouts = Ar.ReadPtrArray(() => new FLayout(Ar));
        if (Version >= 14 && Ar.Game < GAME_UE5_8)
        {
            SkeletonIDs = Ar.ReadArray<uint>();
        }
        if (Ar.Game < GAME_UE5_8) Skeleton = Ar.ReadPtr(() => new FSkeleton(Ar));
        if (Version >= 12)
        {
            PhysicsBody = Ar.ReadPtr(() => new FPhysicsBody(Ar));
        }
        Flags = Ar.Read<EMeshFlags>();
        if (Version >= 16)
        {
            Surfaces = Ar.ReadArray(() => new FMeshSurface(Ar));
        }
        else
        {
            LegacyMeshSurfaces = Ar.ReadArray(() => new FMeshSurfaceLegacy(Ar, true));
        }

        if (Version <= 16)
        {
            FaceGroups = Ar.ReadArray(() => new FACE_GROUP_DEPRECATED(Ar));
        }

        if (Version <= 16)
            Tags = Ar.ReadArray(Ar.ReadString);
        else if (Ar.Game < GAME_UE5_8)
            Tags = Ar.ReadArray(Ar.ReadFString);

        if (Version >= 18 && Ar.Game < GAME_UE5_8)
            StreamedResources = Ar.ReadArray<ulong>();
        if (Version >= 13) BonePoses = Ar.ReadArray(() => new FBonePose(Ar));
        else if (Skeleton is not null)
        {
            // can populate from skeleton
            //for (int32 BoneIndex = 0; BoneIndex < NumBones; ++BoneIndex)
            //{
            //    BonePoses[BoneIndex].BoneId = BoneIndex;
            //    BonePoses[BoneIndex].BoneUsageFlags = EBoneUsageFlags::Skinning;
            //    BonePoses[BoneIndex].BoneTransform = m_pSkeleton->m_boneTransforms_DEPRECATED[BoneIndex];
            //}
        }

        if (Version >= 18)
            BoneMap = Ar.ReadArray<FBoneName>();
        else
        {
            //const int32 NumBonePoses = BonePoses.Num();
            //BoneMap.SetNum(NumBonePoses);
            //for (int32 BoneIndex = 0; BoneIndex < NumBonePoses; ++BoneIndex)
            //{
            //    BoneMap[BoneIndex] = BoneIndex;
            //}

            //for (MESH_SURFACE & Surface : m_surfaces)
            //{
            //    Surface.BoneMapCount = NumBonePoses;
            //}
        }

        if (Version >= 15) AdditionalPhysicsBodies = Ar.ReadArray(() => new FPhysicsBody(Ar));
        if (Ar.Game >= GAME_UE5_5) MeshIDPrefix = Ar.Read<uint>();
        if (Ar.Game < GAME_UE5_6) ReferenceID = Ar.Read<uint>();
        if (Ar.Game >= GAME_UE5_8)
        {
            ClothSections = Ar.ReadArray(() => new FCloth(Ar));
            Morph = new FMeshMorph(Ar);
            MorphDataBuffer = Ar.ReadArray<uint>();
            SkinWeightProfiles = Ar.ReadArray(() => new FSkinWeightProfile(Ar));
        }
    }
}

public class FCloth
{
    public int AssetLODIndex = - 1;
    public FMeshToMeshVertData[] Data;

    public FCloth(FMutableArchive Ar)
    {
        AssetLODIndex = Ar.Read<int>();
        Data = Ar.ReadArray(() => new FMeshToMeshVertData(Ar));
    }
}

[JsonConverter(typeof(StringEnumConverter))]
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
[JsonConverter(typeof(StringEnumConverter))]
public enum EMeshFlags : uint
{
    None = 0,

    /** The mesh is formatted to be used for planar and cilyndrical projection */
    ProjectFormat = 1 << 0,

    /** The mesh is formatted to be used for wrapping projection */
    ProjectWrappingFormat = 1 << 1,
}

public struct FACE_GROUP_DEPRECATED
{
    public string Name;
    public int[] Faces;

    public FACE_GROUP_DEPRECATED(FMutableArchive Ar)
    {
        Ar.Position += 4;
        Name = Ar.ReadString();
        Faces = Ar.ReadArray<int>();
    }
}

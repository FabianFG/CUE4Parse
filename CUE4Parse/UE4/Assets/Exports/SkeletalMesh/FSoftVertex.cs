using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FSoftVertex : FSkelMeshVertexBase
{
    private int MAX_SKELETAL_UV_SETS = 1;

    public sealed override FMeshUVFloat[] UVs { get; }
    public FColor Color;

    public FSoftVertex(FArchive Ar, bool isRigid = false)
    {
        SerializeForEditor(Ar);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_MULTIPLE_UVS_TO_SKELETAL_MESH) MAX_SKELETAL_UV_SETS = 4;
        UVs = Ar.ReadArray<FMeshUVFloat>(MAX_SKELETAL_UV_SETS);

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_SKELETAL_MESH_VERTEX_COLORS)
        {
            Color = Ar.Read<FColor>();
        }

        var len = FSkinWeightInfo.NUM_INFLUENCES_UE4;
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SUPPORT_8_BONE_INFLUENCES_SKELETAL_MESHES) len = FSkinWeightInfo.EXTRA_BONE_INFLUENCES;
        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences) len = FSkinWeightInfo.MAX_TOTAL_INFLUENCES;
        var bUse16BitBoneWeight = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.IncreasedSkinWeightPrecision;

        Infs = !isRigid ?
            new FSkinWeightInfo(Ar, len > FSkinWeightInfo.NUM_INFLUENCES_UE4, true, bUse16BitBoneWeight, len) :
            new FSkinWeightInfo { BoneIndex = { [0] = Ar.Read<byte>() }, BoneWeight = { [0] = 255 } };
    }
}

public class FRigidVertex : FSoftVertex
{
    public FRigidVertex(FArchive Ar) : base(Ar, true) { }
}

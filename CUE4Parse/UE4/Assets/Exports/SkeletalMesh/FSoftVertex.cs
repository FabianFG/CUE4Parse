using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSoftVertex : FSkelMeshVertexBase
    {
        private const int MAX_SKELETAL_UV_SETS_UE4 = 4;

        public FMeshUVFloat[] UV;
        public FColor Color;

        public FSoftVertex(FAssetArchive Ar, bool isRigid = false)
        {
            SerializeForEditor(Ar);

            UV = new FMeshUVFloat[MAX_SKELETAL_UV_SETS_UE4];
            for (var i = 0; i < UV.Length; i++)
                UV[i] = Ar.Read<FMeshUVFloat>();

            Color = Ar.Read<FColor>();
            Infs = !isRigid ?
                new FSkinWeightInfo(Ar, Ar.Ver >= EUnrealEngineObjectUE4Version.SUPPORT_8_BONE_INFLUENCES_SKELETAL_MESHES) :
                new FSkinWeightInfo { BoneIndex = { [0] = Ar.Read<byte>() }, BoneWeight = { [0] = 255 } };
        }
    }

    public class FRigidVertex : FSoftVertex
    {
        public FRigidVertex(FAssetArchive Ar) : base(Ar, true) { }
    }
}

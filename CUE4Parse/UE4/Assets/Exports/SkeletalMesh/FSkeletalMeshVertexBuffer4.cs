using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkeletalMeshVertexBuffer4
    {
        public int NumTexCoords;
        public FVector MeshExtension;
        public FVector MeshOrigin;
        public bool bUseFullPrecisionUVs;
        public bool bExtraBoneInfluences;
        public FGPUVert4Half[]? VertsHalf;
        public FGPUVert4Float[]? VertsFloat;

        public FSkeletalMeshVertexBuffer4()
        {
            
        }
        
        public FSkeletalMeshVertexBuffer4(FAssetArchive Ar)
        {
            var stripDataFlags = new FStripDataFlags(Ar, (int)UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX);

            NumTexCoords = Ar.Read<int>();
            bUseFullPrecisionUVs = Ar.ReadBoolean();
            
            if (Ar.Ver >= UE4Version.VER_UE4_SUPPORT_GPUSKINNING_8_BONE_INFLUENCES &&
                FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.UseSeparateSkinWeightBuffer)
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
            }

            MeshExtension = Ar.Read<FVector>();
            MeshOrigin = Ar.Read<FVector>();

            if (bUseFullPrecisionUVs)
                VertsHalf = Ar.ReadArray(() => new FGPUVert4Half(Ar, NumTexCoords));
            else
                VertsFloat = Ar.ReadArray(() => new FGPUVert4Float(Ar, NumTexCoords));
        }
    }
}
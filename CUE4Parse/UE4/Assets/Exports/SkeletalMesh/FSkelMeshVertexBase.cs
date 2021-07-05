using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkelMeshVertexBase
    {
        public FVector Pos;
        public FPackedNormal[] Normal;
        public FSkinWeightInfo Infs;

        public void SerializeForGPU(FAssetArchive Ar)
        {
            Normal = new FPackedNormal[3];
            Normal[0] = new FPackedNormal(Ar);
            Normal[2] = new FPackedNormal(Ar);
            if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.UseSeparateSkinWeightBuffer)
            {
                // serialized as separate buffer starting with UE4.15
                Infs = new FSkinWeightInfo(Ar, Ar.Ver >= UE4Version.VER_UE4_SUPPORT_8_BONE_INFLUENCES_SKELETAL_MESHES);
            }
            Pos = Ar.Read<FVector>();
        }
        
        public void SerializeForEditor(FAssetArchive Ar)
        {
            Normal = new FPackedNormal[3];
            Pos = Ar.Read<FVector>();
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.IncreaseNormalPrecision)
            {
                Normal[0] = new FPackedNormal(Ar);
                Normal[1] = new FPackedNormal(Ar);
                Normal[2] = new FPackedNormal(Ar);
            }
            else
            {
                // New normals are stored with full floating point precision
                Normal[0] = new FPackedNormal(Ar.Read<FVector>());
                Normal[1] = new FPackedNormal(Ar.Read<FVector>());
                Normal[2] = new FPackedNormal(Ar.Read<FVector4>());
            }
        }
    }
}
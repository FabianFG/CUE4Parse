using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkelMeshChunk4
    {
        public readonly int BaseVertexIndex;
        public readonly FRigidVertex4[] RigidVertices;
        public readonly FSoftVertex4[] SoftVertices;
        public readonly ushort[] BoneMap;
        public readonly int NumRigidVertices;
        public readonly int NumSoftVertices;
        public readonly int MaxBoneInfluences;
        public readonly bool HasClothData;

        public FSkelMeshChunk4(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);
            
            if (!stripDataFlags.IsDataStrippedForServer())
                BaseVertexIndex = Ar.Read<int>();
            
            if (!stripDataFlags.IsEditorDataStripped())
            {
                RigidVertices = Ar.ReadArray(() => new FRigidVertex4(Ar));
                SoftVertices = Ar.ReadArray(() => new FSoftVertex4(Ar));
            }
            
            BoneMap = Ar.ReadArray(Ar.Read<ushort>);
            NumRigidVertices = Ar.Read<int>();
            NumSoftVertices = Ar.Read<int>();
            MaxBoneInfluences = Ar.Read<int>();
            HasClothData = false;
            
            if (Ar.Ver >= UE4Version.VER_UE4_APEX_CLOTH)
            {
                // Physics data, drop
                var clothMappingData = Ar.ReadArray(() => new FApexClothPhysToRenderVertData(Ar));
                Ar.ReadArray(Ar.Read<FVector>); // PhysicalMeshVertices
                Ar.ReadArray(Ar.Read<FVector>); // PhysicalMeshNormals
                Ar.Position += 4; // CorrespondClothAssetIndex, ClothAssetSubmeshIndex
                HasClothData = clothMappingData.Length > 0;
            }
        }
    }
}
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkelMeshSection4
    {
        public readonly short MaterialIndex;
        public readonly int BaseIndex;
        public readonly int NumTriangles;
        public readonly bool bDisabled;
        public readonly short CorrespondClothSectionIndex;
        public readonly int GenerateUpToLodIndex;
        // Data from FSkelMeshChunk, appeared in FSkelMeshSection after UE4.13
        public readonly int NumVertices;
        public readonly uint BaseVertexIndex;
        public readonly FSoftVertex4[] SoftVertices;
        public readonly ushort[] BoneMap;
        public readonly int MaxBoneInfluences;
        public readonly bool HasClothData;
        // UE4.14
        public readonly bool bCastShadow;

        public FSkelMeshSection4(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);

            MaterialIndex = Ar.Read<short>();
            
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
                Ar.Position += 2; // ChunkIndex
            
            if (!stripDataFlags.IsDataStrippedForServer())
            {
                BaseIndex = Ar.Read<int>();
                NumTriangles = Ar.Read<int>();
            }
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveTriangleSorting)
                Ar.Position += 1; // TEnumAsByte<ETriangleSortOption>
            
            if (Ar.Ver >= UE4Version.VER_UE4_APEX_CLOTH)
            {
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.DeprecateSectionDisabledFlag)
                    bDisabled = Ar.ReadBoolean();
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveDuplicatedClothingSections)
                    CorrespondClothSectionIndex = Ar.Read<short>();
            }
            
            if (Ar.Ver >= UE4Version.VER_UE4_APEX_CLOTH_LOD)
                Ar.Position += 1; // bEnableClothLOD_DEPRECATED
            
            if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RuntimeRecomputeTangent)
                Ar.Position += 4; // bRecomputeTangent

            if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RecomputeTangentVertexColorMask)
                Ar.Position += 1; // RecomputeTangentsVertexMaskChannel

            if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.RefactorMeshEditorMaterials)
                bCastShadow = Ar.ReadBoolean();
            
            HasClothData = false;
            if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
            {
                if (!stripDataFlags.IsDataStrippedForServer())
                    BaseVertexIndex = Ar.Read<uint>();
                
                if (!stripDataFlags.IsEditorDataStripped())
                {
                    if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSoftAndRigidVerts)
                        Ar.ReadArray(() => new FRigidVertex4(Ar)); // RigidVertices
                    
                    SoftVertices = Ar.ReadArray(() => new FSoftVertex4(Ar));
                }
                
                BoneMap = Ar.ReadArray(Ar.Read<ushort>);
                if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.SaveNumVertices)
                    NumVertices = Ar.Read<int>();
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSoftAndRigidVerts)
                    Ar.Position += 8; // NumRigidVerts, NumSoftVerts
                MaxBoneInfluences = Ar.Read<int>();

                FVector[] physicalMeshVertices, physicalMeshNormals;
                var clothMappingData = Ar.ReadArray(() => new FApexClothPhysToRenderVertData(Ar));
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveDuplicatedClothingSections)
                {
                    physicalMeshVertices = Ar.ReadArray(Ar.Read<FVector>);
                    physicalMeshNormals = Ar.ReadArray(Ar.Read<FVector>);
                }

                short clothAssetSubmeshIndex;
                var correspondClothAssetIndex = Ar.Read<short>();
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.NewClothingSystemAdded)
                {
                    clothAssetSubmeshIndex = Ar.Read<short>();
                }
                else
                {
                    // UE4.16+
                    Ar.Read<FClothingSectionData>();
                }

                HasClothData = clothMappingData.Length > 0;
                
                if (FOverlappingVerticesCustomVersion.Get(Ar) >= FOverlappingVerticesCustomVersion.Type.DetectOVerlappingVertices)
                {
                    var size = Ar.Read<int>();
                    var overlappingVertices = new Dictionary<int, int[]>();
                    for (var i = 0; i < size; i++)
                    {
                        overlappingVertices[i] = Ar.ReadArray(Ar.Read<int>);
                    }
                }
                if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.AddSkeletalMeshSectionDisable)
                {
                    bDisabled = Ar.ReadBoolean();
                }
                if (FSkeletalMeshCustomVersion.Get(Ar) >= FSkeletalMeshCustomVersion.Type.SectionIgnoreByReduceAdded)
                {
                    GenerateUpToLodIndex = Ar.Read<int>();
                }
            }
        }
    }
}
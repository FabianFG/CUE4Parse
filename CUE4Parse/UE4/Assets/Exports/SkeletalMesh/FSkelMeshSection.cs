using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkelMeshSection
    {
        public short MaterialIndex;
        public int BaseIndex;
        public int NumTriangles;
        public bool bDisabled;
        public short CorrespondClothSectionIndex;
        public int GenerateUpToLodIndex;
        // Data from FSkelMeshChunk, appeared in FSkelMeshSection after UE4.13
        public int NumVertices;
        public uint BaseVertexIndex;
        public FSoftVertex[] SoftVertices;
        public ushort[] BoneMap;
        public int MaxBoneInfluences;
        public bool HasClothData;
        // UE4.14
        public bool bCastShadow;
        
        public FSkelMeshSection()
        {
            SoftVertices = Array.Empty<FSoftVertex>();
        }
        
        public FSkelMeshSection(FAssetArchive Ar) : this()
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
                        Ar.ReadArray(() => new FRigidVertex(Ar)); // RigidVertices
                    
                    SoftVertices = Ar.ReadArray(() => new FSoftVertex(Ar));
                }
                
                BoneMap = Ar.ReadArray<ushort>();
                if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.SaveNumVertices)
                    NumVertices = Ar.Read<int>();
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSoftAndRigidVerts)
                    Ar.Position += 8; // NumRigidVerts, NumSoftVerts
                MaxBoneInfluences = Ar.Read<int>();

                FVector[] physicalMeshVertices, physicalMeshNormals;
                var clothMappingData = Ar.ReadArray(() => new FApexClothPhysToRenderVertData(Ar));
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveDuplicatedClothingSections)
                {
                    physicalMeshVertices = Ar.ReadArray<FVector>();
                    physicalMeshNormals = Ar.ReadArray<FVector>();
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
                        overlappingVertices[i] = Ar.ReadArray<int>();
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

        public void SerializeRenderItem(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            
            MaterialIndex = Ar.Read<short>();
            BaseIndex = Ar.Read<int>();
            NumTriangles = Ar.Read<int>();

            var bRecomputeTangent = Ar.ReadBoolean();
            if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RecomputeTangentVertexColorMask)
            {
                Ar.Position += 1;
            }
            
            bCastShadow = Ar.ReadBoolean();
            BaseVertexIndex = Ar.Read<uint>();
            
            var clothMappingData = Ar.ReadArray(() => new FApexClothPhysToRenderVertData(Ar));
            HasClothData = clothMappingData.Length > 0;
            
            BoneMap = Ar.ReadArray<ushort>();
            NumVertices = Ar.Read<int>();
            MaxBoneInfluences = Ar.Read<int>();

            var correspondClothAssetIndex = Ar.Read<short>();
            var clothingData = Ar.Read<FClothingSectionData>();
            
            if (Ar.Game < EGame.GAME_UE4_23 || !stripDataFlags.IsClassDataStripped(1)) // DuplicatedVertices, introduced in UE4.23
            {
                Ar.SkipFixedArray(4);
                Ar.SkipFixedArray(8);
            }
            bDisabled = Ar.ReadBoolean();
        }
    }
}
using System;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public enum EClassDataStripFlag : byte
    {
        CDSF_AdjacencyData = 1,
        CDSF_MinLodData = 2,
    };
    
    public class FStaticLODModel4
    {
        public FSkelMeshSection4[] Sections;
        public FMultisizeIndexContainer Indices;
        public short[] ActiveBoneIndices;
        public FSkelMeshChunk4[] Chunks;
        public int Size;
        public int NumVertices;
        public short[] RequiredBones;
        public FIntBulkData RawPointIndices;
        public int[] MeshToImportVertexMap;
        public int MaxImportVertex;
        public int NumTexCoords;
        public FSkeletalMeshVertexBuffer4 VertexBufferGPUSkin;
        // public FSkeletalMeshVertexColorBuffer4 ColorVertexBuffer;
        public FMultisizeIndexContainer AdjacencyIndexBuffer;
        public FSkeletalMeshVertexClothBuffer ClothVertexBuffer;

        public FStaticLODModel4()
        {
            Chunks = Array.Empty<FSkelMeshChunk4>();
        }
        
        public FStaticLODModel4(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);

            Sections = new FSkelMeshSection4[Ar.Read<int>()];
            Ar.ReadArray(Sections, () => new FSkelMeshSection4(Ar));
            
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
            {
                Indices = new FMultisizeIndexContainer(Ar);
            }
            else
            {
                // UE4.19+ uses 32-bit index buffer (for editor data)
                Indices = new FMultisizeIndexContainer {Indices32 = Ar.ReadBulkArray(Ar.Read<uint>)};
            }
            
            ActiveBoneIndices = Ar.ReadArray<short>();
            
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
            {
                Chunks = Ar.ReadArray(() => new FSkelMeshChunk4(Ar));
            }

            Size = Ar.Read<int>();
            if (!stripDataFlags.IsDataStrippedForServer())
                NumVertices = Ar.Read<int>();
            
            RequiredBones = Ar.ReadArray<short>();
            if (!stripDataFlags.IsEditorDataStripped())
                RawPointIndices = new FIntBulkData(Ar);
            
            if (Ar.Game != EGame.GAME_SOD2 && Ar.Ver >= UE4Version.VER_UE4_ADD_SKELMESH_MESHTOIMPORTVERTEXMAP)
            {
                MeshToImportVertexMap = Ar.ReadArray<int>();
                MaxImportVertex = Ar.Read<int>();
            }

            if (!stripDataFlags.IsDataStrippedForServer())
            {
                NumTexCoords = Ar.Read<int>();
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
                {
                    VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer4(Ar);
                    if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.UseSeparateSkinWeightBuffer)
                    {
                        var skinWeights = new FSkinWeightVertexBuffer(Ar, VertexBufferGPUSkin.bExtraBoneInfluences);
                        if (skinWeights.Weights.Length > 0)
                        {
                            // Copy data to VertexBufferGPUSkin
                            if (VertexBufferGPUSkin.bUseFullPrecisionUVs)
                            {
                                for (var i = 0; i < NumVertices; i++)
                                {
                                    VertexBufferGPUSkin.VertsFloat[i].Infs = skinWeights.Weights[i];
                                }
                            }
                            else
                            {
                                for (var i = 0; i < NumVertices; i++)
                                {
                                    VertexBufferGPUSkin.VertsHalf[i].Infs = skinWeights.Weights[i];
                                }
                            }
                        }
                    }
                    
                    // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1393
                    
                    if (Ar.Ver < UE4Version.VER_UE4_REMOVE_EXTRA_SKELMESH_VERTEX_INFLUENCES)
                        throw new ParserException("Unsupported: extra SkelMesh vertex influences (old mesh format)");

                    // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1415
                    if (Ar.Game == EGame.GAME_SOD2)
                    {
                        Ar.Position += 8;
                        return;
                    }
                    
                    if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                        AdjacencyIndexBuffer = new FMultisizeIndexContainer(Ar);

                    if (Ar.Ver >= UE4Version.VER_UE4_APEX_CLOTH && HasClothData())
                        ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(Ar);
                }
            }
        }
        
        public void SerializeRenderItem(FAssetArchive Ar)
        {
            if (Ar.Game < EGame.GAME_UE4_24)
            {
                SerializeRenderItem_Legacy(Ar);
                return;
            }
            
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var bIsLODCookedOut = Ar.ReadBoolean();
            var bInlined = Ar.ReadBoolean();
            
            RequiredBones = Ar.ReadArray<short>();
            if (!stripDataFlags.IsDataStrippedForServer() && !bIsLODCookedOut)
            {
                Sections = new FSkelMeshSection4[Ar.Read<int>()];
                for (var i = 0; i < Sections.Length; i++)
                {
                    Sections[i] = new FSkelMeshSection4();
                    Sections[i].SerializeRenderItem(Ar);
                }
                
                ActiveBoneIndices = Ar.ReadArray<short>();
                Ar.Position += 4;
                
                if (bInlined)
                {
                    SerializeStreamedData(Ar);
                }
                else
                {
                    var bulk = new FByteBulkData(Ar);
                    if (bulk.Header.ElementCount > 0)
                    {
                        using (var tempAr = new FAssetArchive(new FByteArchive("LodReader", bulk.Data, Ar.Game, Ar.Ver), Ar.Owner, Ar.AbsoluteOffset))
                        {
                            SerializeStreamedData(tempAr);
                        }
                        
                        var skipBytes = 5;
                        if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                            skipBytes += 5;
                        skipBytes += 4 * 4 + 2 * 4 + 2 * 4;
                        skipBytes += FSkinWeightVertexBuffer.MetadataSize(Ar);
                        Ar.Position += skipBytes;
                        
                        if (HasClothData())
                        {
                            var clothIndexMapping = Ar.ReadArray<long>();
                            Ar.Position += 2 * 4;
                        }
                        var profileNames = Ar.ReadArray(Ar.ReadFName);
                    }
                }
            }
        }

        private void SerializeRenderItem_Legacy(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            
            Sections = new FSkelMeshSection4[Ar.Read<int>()];
            for (var i = 0; i < Sections.Length; i++)
            {
                Sections[i] = new FSkelMeshSection4();
                Sections[i].SerializeRenderItem(Ar);
            }
            
            Indices = new FMultisizeIndexContainer(Ar);
            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer4 {bUseFullPrecisionUVs = true};
            
            ActiveBoneIndices = Ar.ReadArray<short>();
            RequiredBones = Ar.ReadArray<short>();

            if (!stripDataFlags.IsDataStrippedForServer() && !stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_MinLodData))
            {
                var positionVertexBuffer = new FPositionVertexBuffer(Ar);
                var staticMeshVertexBuffer = new FStaticMeshVertexBuffer(Ar);
                var skinWeightVertexBuffer = new FSkinWeightVertexBuffer(Ar, VertexBufferGPUSkin.bExtraBoneInfluences);
                
                // new FColorVertexBuffer(Ar);

                if (!stripDataFlags.IsClassDataStripped((byte) EClassDataStripFlag.CDSF_AdjacencyData))
                    AdjacencyIndexBuffer = new FMultisizeIndexContainer(Ar);

                if (HasClothData())
                    ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(Ar);

                VertexBufferGPUSkin.bUseFullPrecisionUVs = true;
                NumVertices = positionVertexBuffer.NumVertices;
                NumTexCoords = staticMeshVertexBuffer.NumTexCoords;

                VertexBufferGPUSkin.VertsFloat = new FGPUVert4Float[NumVertices];
                for (var i = 0; i < VertexBufferGPUSkin.VertsFloat.Length; i++)
                {
                    VertexBufferGPUSkin.VertsFloat[i] = new FGPUVert4Float
                    {
                        Pos = positionVertexBuffer.Verts[i], Infs = skinWeightVertexBuffer.Weights[i]
                    };
                }
            }
        }

        private void SerializeStreamedData(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            
            Indices = new FMultisizeIndexContainer(Ar);
            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer4 {bUseFullPrecisionUVs = true};

            var positionVertexBuffer = new FPositionVertexBuffer(Ar);
            var staticMeshVertexBuffer = new FStaticMeshVertexBuffer(Ar);
            var skinWeightVertexBuffer = new FSkinWeightVertexBuffer(Ar, VertexBufferGPUSkin.bExtraBoneInfluences);
            
            // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1695
            
            if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                AdjacencyIndexBuffer = new FMultisizeIndexContainer(Ar);

            if (HasClothData())
                ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(Ar);

            // FortniteGame/Content/Accessories/Hats/Mesh/Reindeer_Hat.uasset
            // reads ubulk before getting into this method but not fully
            // so here we are at the end of the archive but not actually
            // will crash even tho we are like half way in the ubulk
            var skinWeightProfilesData = new FSkinWeightProfilesData(Ar);
            
            NumVertices = positionVertexBuffer.NumVertices;
            NumTexCoords = staticMeshVertexBuffer.NumTexCoords;
            
            VertexBufferGPUSkin.VertsFloat = new FGPUVert4Float[NumVertices];
            for (var i = 0; i < VertexBufferGPUSkin.VertsFloat.Length; i++)
            {
                VertexBufferGPUSkin.VertsFloat[i] = new FGPUVert4Float
                {
                    Pos = positionVertexBuffer.Verts[i], Infs = skinWeightVertexBuffer.Weights[i]
                };
            }
        }

        private bool HasClothData()
        {
            for (var i = 0; i < Chunks.Length; i++)
                if (Chunks[i].HasClothData)				// pre-UE4.13 code
                    return true;
            for (var i = 0; i < Sections.Length; i++)	// UE4.13+
                if (Sections[i].HasClothData)
                    return true;
            return false;
        }
    }
}
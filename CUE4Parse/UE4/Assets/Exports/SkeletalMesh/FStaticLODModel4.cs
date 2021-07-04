using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
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
        public readonly FSkelMeshSection4[] Sections;
        public readonly FMultisizeIndexContainer	Indices;
        public readonly short[] ActiveBoneIndices;
        public readonly FSkelMeshChunk4[] Chunks;
        public readonly int Size;
        public readonly int NumVertices;
        public readonly short[] RequiredBones;
        public readonly FIntBulkData RawPointIndices;
        public readonly int[] MeshToImportVertexMap;
        public readonly int MaxImportVertex;
        public readonly int NumTexCoords;
        public readonly FSkeletalMeshVertexBuffer4 VertexBufferGPUSkin;
        // public readonly FSkeletalMeshVertexColorBuffer4 ColorVertexBuffer;
        public readonly FMultisizeIndexContainer AdjacencyIndexBuffer;
        public readonly FSkeletalMeshVertexClothBuffer ClothVertexBuffer;
        
        public FStaticLODModel4(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);

            Sections = Ar.ReadArray(() => new FSkelMeshSection4(Ar));
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
            {
                Indices = new FMultisizeIndexContainer(Ar);
            }
            else
            {
                // UE4.19+ uses 32-bit index buffer (for editor data)
                Indices = new FMultisizeIndexContainer {Indices32 = Ar.ReadBulkArray(Ar.Read<uint>)};
            }
            
            ActiveBoneIndices = Ar.ReadArray(Ar.Read<short>);
            
            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
            {
                Chunks = Ar.ReadArray(() => new FSkelMeshChunk4(Ar));
            }

            Size = Ar.Read<int>();
            if (!stripDataFlags.IsDataStrippedForServer())
                NumVertices = Ar.Read<int>();
            
            RequiredBones = Ar.ReadArray(Ar.Read<short>);
            if (!stripDataFlags.IsEditorDataStripped())
                RawPointIndices = new FIntBulkData(Ar);
            
            if (Ar.Ver >= UE4Version.VER_UE4_ADD_SKELMESH_MESHTOIMPORTVERTEXMAP)
            {
                MeshToImportVertexMap = Ar.ReadArray(Ar.Read<int>);
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
                    
                    if (Ar.Ver < UE4Version.VER_UE4_REMOVE_EXTRA_SKELMESH_VERTEX_INFLUENCES)
                        throw new ParserException("Unsupported: extra SkelMesh vertex influences (old mesh format)");
                    
                    if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                        AdjacencyIndexBuffer = new FMultisizeIndexContainer(Ar);

                    if (Ar.Ver >= UE4Version.VER_UE4_APEX_CLOTH && HasClothData())
                        ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(Ar);
                }
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
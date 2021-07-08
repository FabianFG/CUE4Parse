using System;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public enum EClassDataStripFlag : byte
    {
        CDSF_AdjacencyData = 1,
        CDSF_MinLodData = 2,
    };
    
    [JsonConverter(typeof(FStaticLODModelConverter))]
    public class FStaticLODModel
    {
        public FSkelMeshSection[] Sections;
        public FMultisizeIndexContainer Indices;
        public short[] ActiveBoneIndices;
        public FSkelMeshChunk[] Chunks;
        public int Size;
        public int NumVertices;
        public short[] RequiredBones;
        public FIntBulkData RawPointIndices;
        public int[] MeshToImportVertexMap;
        public int MaxImportVertex;
        public int NumTexCoords;
        public FSkeletalMeshVertexBuffer VertexBufferGPUSkin;
        public FMultisizeIndexContainer AdjacencyIndexBuffer;
        public FSkeletalMeshVertexClothBuffer ClothVertexBuffer;

        public FStaticLODModel()
        {
            Chunks = Array.Empty<FSkelMeshChunk>();
            MeshToImportVertexMap = Array.Empty<int>();
        }
        
        public FStaticLODModel(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);

            Sections = new FSkelMeshSection[Ar.Read<int>()];
            Ar.ReadArray(Sections, () => new FSkelMeshSection(Ar));
            
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
                Chunks = Ar.ReadArray(() => new FSkelMeshChunk(Ar));
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
                    VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer(Ar);
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
                Sections = new FSkelMeshSection[Ar.Read<int>()];
                for (var i = 0; i < Sections.Length; i++)
                {
                    Sections[i] = new FSkelMeshSection();
                    Sections[i].SerializeRenderItem(Ar);
                }
                
                ActiveBoneIndices = Ar.ReadArray<short>();
                Ar.Position += 4; //var buffersSize = Ar.Read<uint>();
                
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
            
            Sections = new FSkelMeshSection[Ar.Read<int>()];
            for (var i = 0; i < Sections.Length; i++)
            {
                Sections[i] = new FSkelMeshSection();
                Sections[i].SerializeRenderItem(Ar);
            }
            
            Indices = new FMultisizeIndexContainer(Ar);
            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer {bUseFullPrecisionUVs = true};
            
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

                NumVertices = positionVertexBuffer.NumVertices;
                NumTexCoords = staticMeshVertexBuffer.NumTexCoords;

                VertexBufferGPUSkin.VertsFloat = new FGPUVertFloat[NumVertices];
                for (var i = 0; i < VertexBufferGPUSkin.VertsFloat.Length; i++)
                {
                    VertexBufferGPUSkin.VertsFloat[i] = new FGPUVertFloat
                    {
                        Pos = positionVertexBuffer.Verts[i],
                        Infs = skinWeightVertexBuffer.Weights[i],
                        Normal = staticMeshVertexBuffer.UV[i].Normal,
                        UV = staticMeshVertexBuffer.UV[i].UV
                    };
                }
            }
        }

        private void SerializeStreamedData(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            
            Indices = new FMultisizeIndexContainer(Ar);
            VertexBufferGPUSkin = new FSkeletalMeshVertexBuffer {bUseFullPrecisionUVs = true};

            var positionVertexBuffer = new FPositionVertexBuffer(Ar);
            var staticMeshVertexBuffer = new FStaticMeshVertexBuffer(Ar);
            var skinWeightVertexBuffer = new FSkinWeightVertexBuffer(Ar, VertexBufferGPUSkin.bExtraBoneInfluences);
            
            // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1695
            
            if (!stripDataFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_AdjacencyData))
                AdjacencyIndexBuffer = new FMultisizeIndexContainer(Ar);

            if (HasClothData())
                ClothVertexBuffer = new FSkeletalMeshVertexClothBuffer(Ar);

            var skinWeightProfilesData = new FSkinWeightProfilesData(Ar);
            
            NumVertices = positionVertexBuffer.NumVertices;
            NumTexCoords = staticMeshVertexBuffer.NumTexCoords;
            
            VertexBufferGPUSkin.VertsFloat = new FGPUVertFloat[NumVertices];
            for (var i = 0; i < VertexBufferGPUSkin.VertsFloat.Length; i++)
            {
                VertexBufferGPUSkin.VertsFloat[i] = new FGPUVertFloat
                {
                    Pos = positionVertexBuffer.Verts[i],
                    Infs = skinWeightVertexBuffer.Weights[i],
                    Normal = staticMeshVertexBuffer.UV[i].Normal,
                    UV = staticMeshVertexBuffer.UV[i].UV
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
    
    public class FStaticLODModelConverter : JsonConverter<FStaticLODModel>
    {
        public override void WriteJson(JsonWriter writer, FStaticLODModel value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("Sections");
            serializer.Serialize(writer, value.Sections);
            
            writer.WritePropertyName("Indices");
            serializer.Serialize(writer, value.Indices);
            
            writer.WritePropertyName("ActiveBoneIndices");
            serializer.Serialize(writer, value.ActiveBoneIndices);
            
            writer.WritePropertyName("NumVertices");
            serializer.Serialize(writer, value.NumVertices);
            
            writer.WritePropertyName("NumTexCoords");
            serializer.Serialize(writer, value.NumTexCoords);
            
            writer.WritePropertyName("RequiredBones");
            serializer.Serialize(writer, value.RequiredBones);
            
            writer.WritePropertyName("VertexBufferGPUSkin");
            serializer.Serialize(writer, value.VertexBufferGPUSkin);

            writer.WritePropertyName("AdjacencyIndexBuffer");
            serializer.Serialize(writer, value.AdjacencyIndexBuffer);

            if (value.Chunks.Length > 0)
            {
                writer.WritePropertyName("Chunks");
                serializer.Serialize(writer, value.Chunks);
                
                writer.WritePropertyName("ClothVertexBuffer");
                serializer.Serialize(writer, value.ClothVertexBuffer);
            }

            if (value.MeshToImportVertexMap.Length > 0)
            {
                writer.WritePropertyName("MeshToImportVertexMap");
                serializer.Serialize(writer, value.MeshToImportVertexMap);
                
                writer.WritePropertyName("MaxImportVertex");
                serializer.Serialize(writer, value.MaxImportVertex);
            }

            writer.WriteEndObject();
        }

        public override FStaticLODModel ReadJson(JsonReader reader, Type objectType, FStaticLODModel existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
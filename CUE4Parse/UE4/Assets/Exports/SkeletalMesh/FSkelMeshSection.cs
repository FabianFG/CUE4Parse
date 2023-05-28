using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public enum ESkinVertexColorChannel : byte
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Alpha = 3,
        None = Alpha
    }

    [JsonConverter(typeof(FSkelMeshSectionConverter))]
    public class FSkelMeshSection
    {
        public short MaterialIndex;
        public int BaseIndex;
        public int NumTriangles;
        public bool bRecomputeTangent;
        public ESkinVertexColorChannel RecomputeTangentsVertexMaskChannel;
        public bool bCastShadow;
        public bool bVisibleInRayTracing;
        [Obsolete]
        public bool bLegacyClothingSection;
        [Obsolete]
        public short CorrespondClothSectionIndex;
        public uint BaseVertexIndex;
        public FSoftVertex[] SoftVertices;
        public FMeshToMeshVertData[][] ClothMappingDataLODs;
        public ushort[] BoneMap;
        public int NumVertices;
        public int MaxBoneInfluences;
        public bool bUse16BitBoneIndex;
        public short CorrespondClothAssetIndex;
        public FClothingSectionData ClothingData;
        public Dictionary<int, int[]> OverlappingVertices;
        public bool bDisabled;
        public int GenerateUpToLodIndex;
        public int OriginalDataSectionIndex;
        public int ChunkedParentSectionIndex;

        public bool HasClothData => ClothMappingDataLODs.Any(data => data.Length > 0);

        public FSkelMeshSection()
        {
            RecomputeTangentsVertexMaskChannel = ESkinVertexColorChannel.None;
            bCastShadow = true;
            bVisibleInRayTracing = true;
            CorrespondClothSectionIndex = -1;
            SoftVertices = Array.Empty<FSoftVertex>();
            ClothMappingDataLODs = Array.Empty<FMeshToMeshVertData[]>();
            MaxBoneInfluences = 4;
            GenerateUpToLodIndex = -1;
            OriginalDataSectionIndex = -1;
            ChunkedParentSectionIndex = -1;
        }

        public FSkelMeshSection(FAssetArchive Ar) : this()
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var skelMeshVer = FSkeletalMeshCustomVersion.Get(Ar);

            MaterialIndex = Ar.Read<short>();

            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
            {
                var dummyChunkIndex = Ar.Read<ushort>();
            }

            if (!stripDataFlags.IsDataStrippedForServer())
            {
                BaseIndex = Ar.Read<int>();
                NumTriangles = Ar.Read<int>();
            }

            if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveTriangleSorting)
            {
                var dummyTriangleSorting = Ar.Read<byte>(); // TEnumAsByte<ETriangleSortOption>
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.APEX_CLOTH)
            {
                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.DeprecateSectionDisabledFlag)
                {
                    bLegacyClothingSection = Ar.ReadBoolean();
                }

                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveDuplicatedClothingSections)
                {
                    CorrespondClothSectionIndex = Ar.Read<short>();
                }
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.APEX_CLOTH_LOD && skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveEnableClothLOD)
            {
                var dummyEnableClothLOD = Ar.Read<byte>();
            }

            if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RuntimeRecomputeTangent)
            {
                bRecomputeTangent = Ar.ReadBoolean();
            }

            RecomputeTangentsVertexMaskChannel = FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RecomputeTangentVertexColorMask ? Ar.Read<ESkinVertexColorChannel>() : ESkinVertexColorChannel.None;
            bCastShadow = FEditorObjectVersion.Get(Ar) < FEditorObjectVersion.Type.RefactorMeshEditorMaterials || Ar.ReadBoolean();
            bVisibleInRayTracing = FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.SkelMeshSectionVisibleInRayTracingFlagAdded || Ar.ReadBoolean();

            if (Ar.Game == EGame.GAME_TrainSimWorld2020) Ar.Position += 8;

            if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.CombineSectionWithChunk)
            {
                if (!stripDataFlags.IsDataStrippedForServer())
                {
                    BaseVertexIndex = Ar.Read<uint>();
                }

                if (!stripDataFlags.IsEditorDataStripped() && !Ar.IsFilterEditorOnly)
                {
                    if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSoftAndRigidVerts)
                    {
                        var legacyRigidVertices = Ar.ReadArray(() => new FRigidVertex(Ar));
                    }

                    SoftVertices = Ar.ReadArray(() => new FSoftVertex(Ar));
                }

                if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk)
                {
                    bUse16BitBoneIndex = Ar.ReadBoolean();
                }

                BoneMap = Ar.ReadArray<ushort>();

                if (skelMeshVer >= FSkeletalMeshCustomVersion.Type.SaveNumVertices)
                {
                    NumVertices = Ar.Read<int>();
                }

                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.CombineSoftAndRigidVerts)
                {
                    var dummyNumRigidVerts = Ar.Read<int>();
                    var dummyNumSoftVerts = Ar.Read<int>();

                    if (dummyNumRigidVerts + dummyNumSoftVerts != SoftVertices.Length)
                    {
                        Log.Error("Legacy NumSoftVerts + NumRigidVerts != SoftVertices.Num()");
                    }
                }

                MaxBoneInfluences = Ar.Read<int>();
                ClothMappingDataLODs = FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.AddClothMappingLODBias ? new[] { Ar.ReadArray(() => new FMeshToMeshVertData(Ar)) } : Ar.ReadArray(() => Ar.ReadArray(() => new FMeshToMeshVertData(Ar)));

                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.RemoveDuplicatedClothingSections)
                {
                    var dummyPhysicalMeshVertices = Ar.ReadArray(() => new FVector(Ar));
                    var dummyPhysicalMeshNormals = Ar.ReadArray(() => new FVector(Ar));
                }

                CorrespondClothAssetIndex = Ar.Read<short>();

                if (skelMeshVer < FSkeletalMeshCustomVersion.Type.NewClothingSystemAdded)
                {
                    var dummyClothAssetSubmeshIndex = Ar.Read<short>();
                }
                else
                {
                    // UE4.16+
                    ClothingData = Ar.Read<FClothingSectionData>();
                }

                if (Ar.Game is EGame.GAME_KingdomHearts3 or EGame.GAME_FinalFantasy7Remake)
                {
                    var shouldReadArray = Ar.Read<int>();
                    var arrayLength = Ar.Read<int>();
                    if (shouldReadArray == 1)
                    {
                        Ar.Position += Ar.Game == EGame.GAME_KingdomHearts3 ? arrayLength * 24 : arrayLength * 16;
                    }
                }

                if (FOverlappingVerticesCustomVersion.Get(Ar) >= FOverlappingVerticesCustomVersion.Type.DetectOVerlappingVertices)
                {
                    var size = Ar.Read<int>();
                    OverlappingVertices = new Dictionary<int, int[]>(size);
                    for (var i = 0; i < size; i++)
                    {
                        OverlappingVertices[Ar.Read<int>()] = Ar.ReadArray<int>();
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
                else
                {
                    GenerateUpToLodIndex = -1;
                }

                if (FEditorObjectVersion.Get(Ar) >= FEditorObjectVersion.Type.SkeletalMeshBuildRefactor)
                {
                    OriginalDataSectionIndex = Ar.Read<int>();
                    ChunkedParentSectionIndex = Ar.Read<int>();
                }
                else
                {
                    OriginalDataSectionIndex = -1;
                    ChunkedParentSectionIndex = -1;
                }
            }
        }

        // Reference: FArchive& operator<<(FArchive& Ar, FSkelMeshRenderSection& S)
        public void SerializeRenderItem(FAssetArchive Ar)
        {
            var stripDataFlags = Ar.Read<FStripDataFlags>();

            MaterialIndex = Ar.Read<short>();
            BaseIndex = Ar.Read<int>();
            NumTriangles = Ar.Read<int>();
            if (Ar.Game == EGame.GAME_Paragon) Ar.Position += 1; // bool
            bRecomputeTangent = Ar.ReadBoolean();
            RecomputeTangentsVertexMaskChannel = FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RecomputeTangentVertexColorMask ? Ar.Read<ESkinVertexColorChannel>() : ESkinVertexColorChannel.None;
            bCastShadow = FEditorObjectVersion.Get(Ar) < FEditorObjectVersion.Type.RefactorMeshEditorMaterials || Ar.ReadBoolean();
            bVisibleInRayTracing = FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.SkelMeshSectionVisibleInRayTracingFlagAdded || Ar.ReadBoolean();
            BaseVertexIndex = Ar.Read<uint>();
            ClothMappingDataLODs = FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.AddClothMappingLODBias ? new[] { Ar.ReadArray(() => new FMeshToMeshVertData(Ar)) } : Ar.ReadArray(() => Ar.ReadArray(() => new FMeshToMeshVertData(Ar)));
            BoneMap = Ar.ReadArray<ushort>();
            NumVertices = Ar.Read<int>();
            MaxBoneInfluences = Ar.Read<int>();
            CorrespondClothAssetIndex = Ar.Read<short>();
            ClothingData = Ar.Read<FClothingSectionData>();

            if (Ar.Game == EGame.GAME_Paragon) return;

            if (Ar.Game < EGame.GAME_UE4_23 || !stripDataFlags.IsClassDataStripped(1)) // DuplicatedVertices, introduced in UE4.23
            {
                Ar.SkipFixedArray(4); // DupVertData
                Ar.SkipFixedArray(8); // DupVertIndexData
            }

            if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.AddSkeletalMeshSectionDisable)
            {
                bDisabled = Ar.ReadBoolean();
            }

            if (Ar.Game == EGame.GAME_OutlastTrials) Ar.Position += 1;
            if (Ar.Game is EGame.GAME_RogueCompany or EGame.GAME_BladeAndSoul) Ar.Position += 4;
        }
    }

    public class FSkelMeshSectionConverter : JsonConverter<FSkelMeshSection>
    {
        public override void WriteJson(JsonWriter writer, FSkelMeshSection value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("MaterialIndex");
            writer.WriteValue(value.MaterialIndex);

            writer.WritePropertyName("BaseIndex");
            writer.WriteValue(value.BaseIndex);

            writer.WritePropertyName("NumTriangles");
            writer.WriteValue(value.NumTriangles);

            writer.WritePropertyName("bRecomputeTangent");
            writer.WriteValue(value.bRecomputeTangent);

            writer.WritePropertyName("RecomputeTangentsVertexMaskChannel");
            writer.WriteValue(value.RecomputeTangentsVertexMaskChannel.ToString());

            writer.WritePropertyName("bCastShadow");
            writer.WriteValue(value.bCastShadow);

            writer.WritePropertyName("bVisibleInRayTracing");
            writer.WriteValue(value.bVisibleInRayTracing);

            writer.WritePropertyName("bLegacyClothingSection");
            writer.WriteValue(value.bLegacyClothingSection);

            writer.WritePropertyName("CorrespondClothSectionIndex");
            writer.WriteValue(value.CorrespondClothSectionIndex);

            writer.WritePropertyName("BaseVertexIndex");
            writer.WriteValue(value.BaseVertexIndex);

            //writer.WritePropertyName("SoftVertices");
            //serializer.Serialize(writer, value.SoftVertices);

            //writer.WritePropertyName("ClothMappingDataLODs");
            //serializer.Serialize(writer, value.ClothMappingDataLODs);

            //writer.WritePropertyName("BoneMap");
            //serializer.Serialize(writer, value.BoneMap);

            writer.WritePropertyName("NumVertices");
            writer.WriteValue(value.NumVertices);

            writer.WritePropertyName("MaxBoneInfluences");
            writer.WriteValue(value.MaxBoneInfluences);

            writer.WritePropertyName("bUse16BitBoneIndex");
            writer.WriteValue(value.bUse16BitBoneIndex);

            writer.WritePropertyName("CorrespondClothAssetIndex");
            writer.WriteValue(value.CorrespondClothAssetIndex);

            //writer.WritePropertyName("ClothingData");
            //serializer.Serialize(writer, value.ClothingData);

            //writer.WritePropertyName("OverlappingVertices");
            //serializer.Serialize(writer, value.OverlappingVertices);

            writer.WritePropertyName("bDisabled");
            writer.WriteValue(value.bDisabled);

            writer.WritePropertyName("GenerateUpToLodIndex");
            writer.WriteValue(value.GenerateUpToLodIndex);

            writer.WritePropertyName("OriginalDataSectionIndex");
            writer.WriteValue(value.OriginalDataSectionIndex);

            writer.WritePropertyName("ChunkedParentSectionIndex");
            writer.WriteValue(value.ChunkedParentSectionIndex);

            writer.WriteEndObject();
        }

        public override FSkelMeshSection ReadJson(JsonReader reader, Type objectType, FSkelMeshSection existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

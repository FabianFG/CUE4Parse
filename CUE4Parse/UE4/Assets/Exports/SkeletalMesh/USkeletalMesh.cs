using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public partial class USkeletalMesh : UObject
{
    public FBoxSphereBounds ImportedBounds { get; private set; }
    public FSkeletalMaterial[] SkeletalMaterials { get; private set; }
    public FReferenceSkeleton ReferenceSkeleton { get; private set; }
    public FSkeletalMeshLODGroupSettings[]? LODInfo { get; private set; }
    public FStaticLODModel[]? LODModels { get; private set; }
    public bool bHasVertexColors { get; private set; }
    public byte NumVertexColorChannels { get; private set; }
    public FPackageIndex[] MorphTargets { get; private set; }
    public FPackageIndex[] Sockets { get; private set; }
    public FPackageIndex Skeleton { get; private set; }
    public FPackageIndex?[] Materials { get; private set; } = []; // UMaterialInterface[]
    public bool bEnablePerPolyCollision { get; private set; }
    public FPackageIndex PhysicsAsset { get; private set; }
    public FPackageIndex[]? AssetUserData { get; private set; }
    public FNaniteResources? NaniteResources;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 8;
        base.Deserialize(Ar, validPos);
        LODInfo = GetOrDefault<FSkeletalMeshLODGroupSettings[]?>(nameof(LODInfo)) ?? GetOrDefault<FSkeletalMeshLODGroupSettings[]>("SourceModels");

        bHasVertexColors = GetOrDefault<bool>(nameof(bHasVertexColors));
        NumVertexColorChannels = GetOrDefault<byte>(nameof(NumVertexColorChannels));
        MorphTargets = GetOrDefault(nameof(MorphTargets), Array.Empty<FPackageIndex>());
        Sockets = GetOrDefault(nameof(Sockets), Array.Empty<FPackageIndex>());
        Skeleton = GetOrDefault(nameof(Skeleton), new FPackageIndex());
        bEnablePerPolyCollision = GetOrDefault<bool>(nameof(bEnablePerPolyCollision));
        PhysicsAsset = GetOrDefault(nameof(PhysicsAsset), new FPackageIndex());
        AssetUserData = GetOrDefault(nameof(AssetUserData), Array.Empty<FPackageIndex>());

        var stripDataFlags = new FStripDataFlags(Ar);

        if (Ar.Game == GAME_Dishonored && Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_SCALES2)
        {
            Ar.SkipFName(); // m_BoneName
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.OPTIMIZED_ANIMSEQ) Ar.Position += sizeof(float) * 3; // FVector - m_Offset
            Ar.Position += sizeof(float); // float - m_fRadius
        }

        ImportedBounds = new FBoxSphereBounds(Ar);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.DeprecatedPointer)
        {
            Ar.Position += sizeof(int); // FPackageIndex
        }

        if (Ar.Game < GAME_UE4_0)
        {
            Materials = Ar.ReadArray(() => new FPackageIndex(Ar));

            SkeletalMaterials = new FSkeletalMaterial[Materials.Length];
            for (var i = 0; i < Materials.Length; i++)
            {
                SkeletalMaterials[i] = new FSkeletalMaterial(Materials[i]);
            }

            Ar.Position += sizeof(float) * 3; // FVector - MeshOrigin
            Ar.Position += sizeof(float) * 3; // FRotator - RotOrigin
            if (Ar.Game == GAME_Dishonored && Ar.Ver >= EUnrealEngineObjectUE3Version.FIXCLAMP_NON_TONEMAP)
            {
                Ar.SkipArray<byte>();
            }
        }
        else
        {
            SkeletalMaterials = Ar.ReadArray(() => new FSkeletalMaterial(Ar));
            Materials = new FPackageIndex?[SkeletalMaterials.Length];
            for (var i = 0; i < Materials.Length; i++)
            {
                Materials[i] = SkeletalMaterials[i].Material;
            }
        }

        if (Ar.Game is GAME_LordOfMysteries) CustomGameData = Ar.ReadArray(() => new FSkeletalMaterial(Ar));

        ReferenceSkeleton = new FReferenceSkeleton(Ar);
        if (Ar.Game < GAME_UE4_0)
        {
            Ar.Position += sizeof(int); // int - SkeletalDepth
        }

        if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
        {
            LODModels = Ar.Game switch
            {
                GAME_GameForPeace => GFPSerializeLODModels(Ar),
                _ => Ar.ReadArray(() => new FStaticLODModel(Ar, bHasVertexColors)),
            };
        }
        else
        {
            if (!stripDataFlags.IsEditorDataStripped())
            {
                LODModels = Ar.ReadArray(() => new FStaticLODModel(Ar, bHasVertexColors));
            }

            var bCooked = Ar.ReadBoolean();
            if (Ar.Versions["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"])
            {
                var minMobileLODIdx = Ar.Read<int>();
            }

            if (Ar.Game is GAME_GearsofWarEDay)
            {
                Ar.SkipBulkArrayData();
                Ar.SkipArray<uint>();
                var count = Ar.Read<int>();
                for (var i = 0; i < count; i++)
                {
                    Ar.Position += 8;
                    Ar.SkipArray<uint>();
                    Ar.Position += 4;
                }
                Ar.Position += 32;
            }

            if (bCooked && LODModels == null)
            {
                var useNewCookedFormat = Ar.Versions["SkeletalMesh.UseNewCookedFormat"];
                LODModels = new FStaticLODModel[Ar.Read<int>()];
                for (var i = 0; i < LODModels.Length; i++)
                {
                    LODModels[i] = new FStaticLODModel();
                    if (useNewCookedFormat)
                    {
                        LODModels[i].SerializeRenderItem(Ar, bHasVertexColors, NumVertexColorChannels);
                    }
                    else
                    {
                        LODModels[i].SerializeRenderItem_Legacy(Ar, bHasVertexColors, NumVertexColorChannels);
                    }
                }

                if (Ar.Game == GAME_Stalker2)
                {
                    var fallbackLODModels = new FStaticLODModel[Ar.Read<int>()];
                    for (var i = 0; i < fallbackLODModels.Length; i++)
                    {
                        fallbackLODModels[i] = new FStaticLODModel();
                        fallbackLODModels[i].SerializeRenderItem(Ar, bHasVertexColors, NumVertexColorChannels);
                    }

                    LODModels = LODModels.Concat(fallbackLODModels).ToArray();
                }

                if (Ar.Game is GAME_RocoKingdomWorld)
                {
                    foreach (var lod in LODModels)
                    {
                        for (int i = 0; i < lod.VertexBufferGPUSkin.VertsFloat.Length; i++)
                        {
                            var vert = lod.VertexBufferGPUSkin.VertsFloat[i];
                            vert.Pos = ImportedBounds.BoxExtent * vert.Pos + ImportedBounds.Origin;
                        }
                    }
                }

                if (Ar.Game >= GAME_UE5_5 || Ar.Game is GAME_SilverPalace)
                {
                    NaniteResources = new FNaniteResources(Ar);
                }

                if (Ar.Game == GAME_DeadzoneRogue) Ar.Position += 4;

                if (useNewCookedFormat)
                {
                    var numInlinedLODs = Ar.Read<byte>();
                    var numNonOptionalLODs = Ar.Read<byte>();
                }
            }
        }

        if (Ar.Game == GAME_WorldofJadeDynasty)
        {
            _ = new FStripDataFlags(Ar);
            for (var i = 0; i < LODModels.Length; i++)
            {
                if (Ar.ReadBoolean() && GetOrDefault<bool>("bGenerateMeshDistanceField")) _ = new FDistanceFieldVolumeData5(Ar);
            }
        }

        if (Ar.Game is GAME_GearsofWarEDay && Ar.ReadBoolean())
        {
            Ar.Position += 16;
            Ar.SkipMultipleFixedArrays(Ar.Read<int>(), 3);
            Ar.SkipMultipleFixedArrays(Ar.Read<int>(), 32);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADD_SKELMESH_NAMEINDEXMAP && Ar.Ver < EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
        {
            var length = Ar.Read<int>();
            Ar.Position += 12 * length; // TMap<FName, int32> DummyNameIndexMap
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SKELMESH_BONE_KDOP && Ar.Game < GAME_UE4_0)
        {
            // this is not an array of ints, it's a complex FPerPolyBoneCollisionData struct
            Ar.SkipArray<int>(); // PerPolyBoneKDOPs
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_EXTRA_SKELMESH_VERTEX_INFLUENCE_MAPPING && Ar.Game < GAME_UE4_0)
        {
            Ar.SkipArray(Ar.SkipFString); // BoneBreakNames
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_EXTRA_SKELMESH_VERTEX_INFLUENCE_CUSTOM_MAPPING)
            {
                Ar.SkipArray<int>(); // BoneBreakOptions
            }
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.APEX_CLOTHING && Ar.Game < GAME_UE4_0)
        {
            var ApexClothingAssets = Ar.Read<int>();
            for (var i = 0; i < ApexClothingAssets; i++)
            {
                var bAssetValid = Ar.ReadBoolean();

                if (bAssetValid)
                {
                    Ar.SkipArray<byte>(); // NameBuffer
                    Ar.SkipArray<byte>(); // Buffer
                }
            }
        }

        switch (Ar.Game)
        {
            case GAME_Back4Blood:
                Ar.Position += 8;
                break;
            case >= GAME_UE4_0:
                _ = Ar.ReadArray(() => new FPackageIndex(Ar)); // dummyObjs
                break;
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DYNAMICTEXTUREINSTANCES && FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
        {
            Ar.SkipFixedArray(sizeof(float));
        }

        if ((Ar.Game >= GAME_UE4_19 && !Ar.IsFilterEditorOnly) || Ar.Game < GAME_UE4_19)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.APEX_CLOTH)
            {
                if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.NewClothingSystemAdded)
                {
                    var num = GetOrDefault("ClothingAssets", Array.Empty<StructProperty>()).Length;
                    Ar.SkipMultipleFixedArrays(num, 1);
                }
            }
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SKELETAL_MESH_SIMPLIFICATION && Ar.Game < GAME_UE4_0)
        {
            var bHaveSourceData = Ar.ReadBoolean();
            if (bHaveSourceData)
            {
                new FStaticLODModel(Ar, bHasVertexColors);
            }
        }
        // if (bEnablePerPolyCollision)
        // {
        //     var bodySetup = new FPackageIndex(Ar);
        // }

        if (Ar.Game == GAME_OutlastTrials) Ar.Position += 1;
        if (Ar.Game == GAME_WeHappyFew) Ar.Position += 20;

        if (TryGetValue(out FStructFallback[] lodInfos, "LODInfo"))
        {
            for (var i = 0; i < LODModels?.Length; i++)
            {
                var lodInfo = i < lodInfos.Length ? lodInfos[i] : null;
                if (lodInfo is null || !lodInfo.TryGetValue(out int[] lodMatMap, "LODMaterialMap")) continue;

                var lodModel = LODModels[i];
                for (var j = 0; j < lodModel.Sections.Length; j++)
                {
                    if (j < lodMatMap.Length && lodMatMap[j] >= 0 && lodMatMap[j] < SkeletalMaterials.Length)
                    {
                        lodModel.Sections[j].MaterialIndex = (short) Math.Clamp((ushort) lodMatMap[j], 0, SkeletalMaterials.Length);
                    }
                }
            }
        }
    }

    public void PopulateMorphTargetVerticesData()
    {
        if (LODModels is null || MorphTargets.Length == 0) return;

        if (Owner?.Provider?.Versions.Game is GAME_MortalKombat1)
        {
            PopulateMorphTargetVerticesDataMK1();
            return;
        }

        var maxLodLevel = -1;
        for (int i = 0; i < LODModels.Length; i++)
        {
            if (LODModels[i].MorphTargetVertexInfoBuffers is not null)
            {
                maxLodLevel = i + 1;
            }
        }

        if (maxLodLevel == -1) return;

        for (int index = 0; index < MorphTargets.Length; index++)
        {
            if (!MorphTargets[index].TryLoad<UMorphTarget>(out var morphTarget))
                continue;

            var morphLODModels = morphTarget.MorphLODModels;
            if (morphLODModels.Length == 0)
            {
                var morphTargetLODModels = new FMorphTargetLODModel[maxLodLevel];
                for (var i = 0; i < maxLodLevel; i++)
                {
                    if (LODModels[i].MorphTargetVertexInfoBuffers is null || LODModels[i].MorphTargetVertexInfoBuffers!.BatchesPerMorph[index] == 0)
                        morphTargetLODModels[i] = new FMorphTargetLODModel();

                    morphTargetLODModels[i] = new FMorphTargetLODModel(LODModels[i].MorphTargetVertexInfoBuffers!, index, []);
                }

                morphTarget.MorphLODModels = morphTargetLODModels;
                continue;
            }

            for (int j = 0; j < morphLODModels.Length; j++)
            {
                if (morphTarget.TryGetCompressedLODModel(j, out var compressedLodModel))
                {
                    morphLODModels[j] = new FMorphTargetLODModel(morphLODModels[j].SectionIndices, compressedLodModel.PackedDeltaHeaders, compressedLodModel.PackedDeltaData, compressedLodModel.PositionPrecision, compressedLodModel.TangentPrecision);
                }
                else
                {
                    if (morphLODModels[j].Vertices.Length > 0 || morphLODModels[j].NumBaseMeshVerts == 0 || morphLODModels[j].SectionIndices.Length == 0 || j >= LODModels.Length || LODModels[j].MorphTargetVertexInfoBuffers is null) continue;
                    morphLODModels[j] = new FMorphTargetLODModel(LODModels[j].MorphTargetVertexInfoBuffers!, index, morphLODModels[j].SectionIndices);
                }
            }

            if (morphLODModels.Length >= maxLodLevel) continue;

            var newMorphLods = new FMorphTargetLODModel[maxLodLevel];
            Array.Copy(morphLODModels, newMorphLods, morphLODModels.Length);
            for (int j = morphLODModels.Length; j < maxLodLevel; j++)
            {
                if (j < LODModels.Length && LODModels[j].MorphTargetVertexInfoBuffers is not null)
                    newMorphLods[j] = new FMorphTargetLODModel(LODModels[j].MorphTargetVertexInfoBuffers!, index, []);
                else
                    newMorphLods[j] = new FMorphTargetLODModel();
            }

            morphTarget.MorphLODModels = newMorphLods;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(ImportedBounds));
        serializer.Serialize(writer, ImportedBounds);

        writer.WritePropertyName(nameof(SkeletalMaterials));
        serializer.Serialize(writer, SkeletalMaterials);

        writer.WritePropertyName(nameof(LODModels));
        serializer.Serialize(writer, LODModels);

        writer.WritePropertyName(nameof(NaniteResources));
        serializer.Serialize(writer, NaniteResources);
    }
}

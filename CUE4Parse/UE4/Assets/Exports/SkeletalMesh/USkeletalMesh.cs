using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class USkeletalMesh : UObject
{
    public FBoxSphereBounds ImportedBounds { get; private set; }
    public FSkeletalMaterial[] SkeletalMaterials { get; private set; }
    public FReferenceSkeleton ReferenceSkeleton { get; private set; }
    public FStaticLODModel[]? LODModels { get; private set; }
    public bool bHasVertexColors { get; private set; }
    public byte NumVertexColorChannels { get; private set; }
    public FPackageIndex[] MorphTargets { get; private set; }
    public FPackageIndex[] Sockets { get; private set; }
    public FPackageIndex Skeleton { get; private set; }
    public ResolvedObject?[] Materials { get; private set; } // UMaterialInterface[]
    public FPackageIndex PhysicsAsset { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Materials = [];

        bHasVertexColors = GetOrDefault<bool>(nameof(bHasVertexColors));
        NumVertexColorChannels = GetOrDefault<byte>(nameof(NumVertexColorChannels));
        MorphTargets = GetOrDefault(nameof(MorphTargets), Array.Empty<FPackageIndex>());
        Sockets = GetOrDefault(nameof(Sockets), Array.Empty<FPackageIndex>());
        Skeleton = GetOrDefault(nameof(Skeleton), new FPackageIndex());
        PhysicsAsset = GetOrDefault(nameof(PhysicsAsset), new FPackageIndex());

        var stripDataFlags = Ar.Read<FStripDataFlags>();
        ImportedBounds = new FBoxSphereBounds(Ar);

        SkeletalMaterials = Ar.ReadArray(() => new FSkeletalMaterial(Ar));
        Materials = new ResolvedObject?[SkeletalMaterials.Length];
        for (var i = 0; i < Materials.Length; i++)
        {
            Materials[i] = SkeletalMaterials[i].Material;
        }

        ReferenceSkeleton = new FReferenceSkeleton(Ar);

        if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
        {
            LODModels = Ar.ReadArray(() => new FStaticLODModel(Ar, bHasVertexColors));
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

                if (Ar.Game == EGame.GAME_Stalker2)
                {
                    var fallbackLODModels = new FStaticLODModel[Ar.Read<int>()];
                    for (var i = 0; i < fallbackLODModels.Length; i++)
                    {
                        fallbackLODModels[i] = new FStaticLODModel();
                        fallbackLODModels[i].SerializeRenderItem(Ar, bHasVertexColors, NumVertexColorChannels);
                    }

                    LODModels = LODModels.Concat(fallbackLODModels).ToArray();
                }

                if (Ar.Game >= EGame.GAME_UE5_5)
                {
                    var NaniteResources = new FNaniteResources(Ar);
                }

                if (useNewCookedFormat)
                {
                    var numInlinedLODs = Ar.Read<byte>();
                    var numNonOptionalLODs = Ar.Read<byte>();
                }
            }
        }

        if (Ar.Ver < EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
        {
            var length = Ar.Read<int>();
            Ar.Position += 12 * length; // TMap<FName, int32> DummyNameIndexMap
        }

        var dummyObjs = Ar.ReadArray(() => new FPackageIndex(Ar));

        if (Ar.Game == EGame.GAME_OutlastTrials) Ar.Position += 1;

        if (TryGetValue(out FStructFallback[] lodInfos, "LODInfo"))
        {
            for (var i = 0; i < LODModels?.Length; i++)
            {
                var lodInfo = i < lodInfos.Length ? lodInfos[i] : null;
                if (lodInfo is null || !lodInfo.TryGetValue(out int[] lodMatMap, "LODMaterialMap")) continue;

                var lodModel = LODModels[i];
                for (var j = 0; j < lodModel.Sections.Length; j++)
                {
                    if (j < lodMatMap.Length && lodMatMap[j] >= 0 && lodMatMap[j] < Materials.Length)
                    {
                        lodModel.Sections[j].MaterialIndex = (short) Math.Clamp((ushort) lodMatMap[j], 0, Materials.Length);
                    }
                }
            }
        }
    }

    public void PopulateMorphTargetVerticesData()
    {
        if (LODModels is null || MorphTargets.Length == 0) return;

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
            if (!MorphTargets[index].TryLoad(out UMorphTarget morphTarget)) continue;

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
                if (morphLODModels[j].Vertices.Length > 0 || morphLODModels[j].NumBaseMeshVerts == 0 || morphLODModels[j].SectionIndices.Length == 0) continue;
                morphLODModels[j] = new FMorphTargetLODModel(LODModels[j].MorphTargetVertexInfoBuffers!, index, morphLODModels[j].SectionIndices);
            }

            if (morphLODModels.Length >= maxLodLevel) continue;

            var newMorphLods = new FMorphTargetLODModel[maxLodLevel];
            Array.Copy(morphLODModels, newMorphLods, morphLODModels.Length);
            for (int j = morphLODModels.Length; j < maxLodLevel; j++)
            {
                newMorphLods[j] = new FMorphTargetLODModel(LODModels[j].MorphTargetVertexInfoBuffers!, index, []);
            }

            morphTarget.MorphLODModels = newMorphLods;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ImportedBounds");
        serializer.Serialize(writer, ImportedBounds);

        writer.WritePropertyName("SkeletalMaterials");
        serializer.Serialize(writer, SkeletalMaterials);

        writer.WritePropertyName("LODModels");
        serializer.Serialize(writer, LODModels);
    }
}

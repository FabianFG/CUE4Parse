using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
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

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Materials = Array.Empty<ResolvedObject>();

            bHasVertexColors = GetOrDefault<bool>(nameof(bHasVertexColors));
            NumVertexColorChannels = GetOrDefault<byte>(nameof(NumVertexColorChannels));
            MorphTargets = GetOrDefault(nameof(MorphTargets), Array.Empty<FPackageIndex>());
            Sockets = GetOrDefault(nameof(Sockets), Array.Empty<FPackageIndex>());
            Skeleton = GetOrDefault(nameof(Skeleton), new FPackageIndex());

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

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("ImportedBounds");
            serializer.Serialize(writer, ImportedBounds);

            writer.WritePropertyName("Materials");
            serializer.Serialize(writer, Materials);

            writer.WritePropertyName("LODModels");
            serializer.Serialize(writer, LODModels);
        }
    }
}

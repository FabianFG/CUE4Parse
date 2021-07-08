using CUE4Parse.UE4.Assets.Exports.Animation;
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
        public FSkeletalMaterial[] Materials { get; private set; }
        public FReferenceSkeleton ReferenceSkeleton { get; private set; }
        public FStaticLODModel[]? LODModels { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var stripDataFlags = Ar.Read<FStripDataFlags>();
            ImportedBounds = Ar.Read<FBoxSphereBounds>();
            Materials = Ar.ReadArray(() => new FSkeletalMaterial(Ar));
            ReferenceSkeleton = new FReferenceSkeleton(Ar);

            if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
            {
                LODModels = Ar.ReadArray(() => new FStaticLODModel(Ar));
            }
            else
            {
                if (!stripDataFlags.IsEditorDataStripped())
                {
                    LODModels = Ar.ReadArray(() => new FStaticLODModel(Ar));
                }

                var bCooked = Ar.ReadBoolean();
                if (Ar.Game >= EGame.GAME_UE4_27)
                {
                    var minMobileLODIdx = Ar.Read<int>();
                }

                if (bCooked && LODModels == null)
                {
                    LODModels = new FStaticLODModel[Ar.Read<int>()];
                    for (var i = 0; i < LODModels.Length; i++)
                    {
                        var lodModel = new FStaticLODModel();
                        lodModel.SerializeRenderItem(Ar);
                        LODModels[i] = lodModel;
                    }

                    var numInlinedLODs = Ar.Read<byte>();
                    var numNonOptionalLODs = Ar.Read<byte>();
                }
            }

            if (Ar.Ver < UE4Version.VER_UE4_REFERENCE_SKELETON_REFACTOR)
            {
                var length = Ar.Read<int>();
                Ar.Position += 12 * length; // TMap<FName, int32> DummyNameIndexMap
            }

            var dummyObjs = Ar.ReadArray(() => new FPackageIndex(Ar));
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
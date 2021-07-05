using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class USkeletalMesh : UObject
    {
        public FBoxSphereBounds ImportedBounds { get; private set; }
        public FSkeletalMaterial[] Materials { get; private set; }
        public FReferenceSkeleton ReferenceSkeleton { get; private set; }
        // public USkeleton Skeleton { get; private set; }
        public FStaticLODModel4[]? LODModels { get; private set; }
        // public FSkeletalMeshLODInfo[] LODInfo { get; private set; }
        // public UMorphTarget[] MorphTargets { get; private set; }
        // public USkeletalMeshSocket[] Sockets { get; private set; }
        // public FSkeletalMeshSamplingInfo SamplingInfo { get; private set; }
        // public FSkinWeightProfileInfo[] SkinWeightProfiles { get; private set; }
        
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            
            var stripDataFlags = Ar.Read<FStripDataFlags>();
            ImportedBounds = Ar.Read<FBoxSphereBounds>();
            Materials = Ar.ReadArray(() => new FSkeletalMaterial(Ar));
            ReferenceSkeleton = new FReferenceSkeleton(Ar);

            if (FSkeletalMeshCustomVersion.Get(Ar) < FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
            {
                LODModels = Ar.ReadArray(() => new FStaticLODModel4(Ar));
            }
            else
            {
                if (!stripDataFlags.IsEditorDataStripped())
                {
                    LODModels = Ar.ReadArray(() => new FStaticLODModel4(Ar));
                }
                
                var bCooked = Ar.ReadBoolean();
                if (Ar.Game >= EGame.GAME_UE4_27)
                {
                    var minMobileLODIdx = Ar.Read<int>();
                }

                if (bCooked && LODModels == null)
                {
                    // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1845
                    // serialize cooked data only if editor data not exists - use custom array serializer function
                    // LODModels.Serialize2<FStaticLODModel4::SerializeRenderItem>(Ar);
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
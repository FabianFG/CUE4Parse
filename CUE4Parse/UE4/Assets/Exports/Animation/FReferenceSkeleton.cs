using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(FReferenceSkeletonConverter))]
    public class FReferenceSkeleton
    {
        public readonly FMeshBoneInfo[] FinalRefBoneInfo;
        public readonly FTransform[] FinalRefBonePose;
        public readonly Dictionary<string, int> FinalNameToIndexMap;

        public FReferenceSkeleton(FAssetArchive Ar)
        {
            FinalRefBoneInfo = Ar.ReadArray(() => new FMeshBoneInfo(Ar));
            FinalRefBonePose = Ar.ReadArray(() => new FTransform(Ar));

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
            {
                var num = Ar.Read<int>();
                FinalNameToIndexMap = new Dictionary<string, int>(num);
                for (var i = 0; i < num; ++i)
                {
                    FinalNameToIndexMap[Ar.ReadFName().Text] = Ar.Read<int>();
                }
            }
            else FinalNameToIndexMap = new Dictionary<string, int>();

            if (Ar.Ver < EUnrealEngineObjectUE4Version.FIXUP_ROOTBONE_PARENT)
            {
                if (FinalRefBoneInfo.Length > 0 && FinalRefBoneInfo[0].ParentIndex != -1)
                {
                    FinalRefBoneInfo[0] = new FMeshBoneInfo(FinalRefBoneInfo[0].Name, -1);
                }
            }

            AdjustBoneScales(FinalRefBonePose);
        }

        public void AdjustBoneScales(FTransform[] transforms)
        {
            if (FinalRefBoneInfo.Length != transforms.Length)
                return;

            for (int boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
            {
                var scale = GetBoneScale(transforms, boneIndex);
                transforms[boneIndex].Translation.Scale(scale);
            }
        }

        public FVector GetBoneScale(FTransform[] transforms, int boneIndex)
        {
            var scale = new FVector(1);

            // Get the parent bone, ignore scale of the current one
            boneIndex = FinalRefBoneInfo[boneIndex].ParentIndex;
            while (boneIndex >= 0)
            {
                var boneScale = transforms[boneIndex].Scale3D;
                // Accumulate the scale
                scale.Scale(boneScale);
                // Get the bone's parent
                boneIndex = FinalRefBoneInfo[boneIndex].ParentIndex;
            }

            return scale;
        }
    }

    public class FReferenceSkeletonConverter : JsonConverter<FReferenceSkeleton>
    {
        public override void WriteJson(JsonWriter writer, FReferenceSkeleton value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("FinalRefBoneInfo");
            writer.WriteStartArray();
            {
                foreach (var boneInfo in value.FinalRefBoneInfo)
                {
                    serializer.Serialize(writer, boneInfo);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName("FinalRefBonePose");
            writer.WriteStartArray();
            {
                foreach (var bonePose in value.FinalRefBonePose)
                {
                    serializer.Serialize(writer, bonePose);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName("FinalNameToIndexMap");
            serializer.Serialize(writer, value.FinalNameToIndexMap);

            writer.WriteEndObject();
        }

        public override FReferenceSkeleton ReadJson(JsonReader reader, Type objectType, FReferenceSkeleton existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

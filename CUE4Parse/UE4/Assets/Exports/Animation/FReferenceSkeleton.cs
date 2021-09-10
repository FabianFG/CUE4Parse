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
        public readonly Dictionary<FName, int>? FinalNameToIndexMap;

        public FReferenceSkeleton(FAssetArchive Ar)
        {
            FinalRefBoneInfo = Ar.ReadArray(() => new FMeshBoneInfo(Ar));
            FinalRefBonePose = Ar.ReadArray<FTransform>();

            if (Ar.Ver >= UE4Version.VER_UE4_REFERENCE_SKELETON_REFACTOR)
            {
                var num = Ar.Read<int>();
                FinalNameToIndexMap = new Dictionary<FName, int>(num);
                for (var i = 0; i < num; ++i)
                {
                    FinalNameToIndexMap[Ar.ReadFName()] = Ar.Read<int>();
                }
            }

            if (Ar.Ver < UE4Version.VER_UE4_FIXUP_ROOTBONE_PARENT)
            {
                if (FinalRefBoneInfo.Length > 0 && FinalRefBoneInfo[0].ParentIndex != -1)
                {
                    FinalRefBoneInfo[0] = new FMeshBoneInfo(FinalRefBoneInfo[0].Name, - 1);
                }
            }
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

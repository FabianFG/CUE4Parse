using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(FReferencePoseConverter))]
    public struct FReferencePose
    {
        public readonly FName PoseName;
        public readonly FTransform[] ReferencePose;

        public FReferencePose(FAssetArchive Ar)
        {
            PoseName = Ar.ReadFName();
            ReferencePose = Ar.ReadArray(() => new FTransform(Ar));
        }
    }

    public class FReferencePoseConverter : JsonConverter<FReferencePose>
    {
        public override void WriteJson(JsonWriter writer, FReferencePose value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("PoseName");
            serializer.Serialize(writer, value.PoseName);

            writer.WritePropertyName("ReferencePose");
            writer.WriteStartArray();
            {
                foreach (var pose in value.ReferencePose)
                {
                    serializer.Serialize(writer, pose);
                }
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override FReferencePose ReadJson(JsonReader reader, Type objectType, FReferencePose existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

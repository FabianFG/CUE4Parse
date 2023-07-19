using System;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Curves
{
    [JsonConverter(typeof(FCurveMetaDataConverter))]
    public class FCurveMetaData
    {
        public readonly FAnimCurveType Type;
        public readonly FName[] LinkedBones;
        public readonly int MaxLOD;

        public FCurveMetaData(FArchive Ar, FAnimPhysObjectVersion.Type FrwAniVer)
        {
            Type = new FAnimCurveType(Ar);
            LinkedBones = Ar.ReadArray(Ar.ReadFName);
            if (FrwAniVer >= FAnimPhysObjectVersion.Type.AddLODToCurveMetaData)
            {
                MaxLOD = Ar.Game == EGame.GAME_KingdomHearts3 ? Ar.Read<int>() : Ar.Read<byte>();
            }

            if (Ar.Game == EGame.GAME_FinalFantasy7Remake)
            {
                // Cutscene mat replacements
                var matAssetName = Ar.ReadFName();
                var matName = Ar.ReadFName();
            }
        }
    }

    public class FCurveMetaDataConverter : JsonConverter<FCurveMetaData>
    {
        public override void WriteJson(JsonWriter writer, FCurveMetaData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Type");
            serializer.Serialize(writer, value.Type);

            writer.WritePropertyName("LinkedBones");
            writer.WriteStartArray();
            foreach (var bone in value.LinkedBones)
            {
                serializer.Serialize(writer, bone);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("MaxLOD");
            writer.WriteValue(value.MaxLOD);

            writer.WriteEndObject();
        }

        public override FCurveMetaData ReadJson(JsonReader reader, Type objectType, FCurveMetaData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
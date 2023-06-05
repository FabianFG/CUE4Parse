using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    [JsonConverter(typeof(FTexture2DMipMapConverter))]
    public class FTexture2DMipMap
    {
        public readonly FByteBulkData BulkData;
        public int SizeX;
        public int SizeY;
        public readonly int SizeZ;

        public FTexture2DMipMap(FAssetArchive Ar)
        {
            var cooked = Ar.Ver >= EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR && Ar.Game < EGame.GAME_UE5_0 ? Ar.ReadBoolean() : Ar.IsFilterEditorOnly;

            BulkData = new FByteBulkData(Ar);

            if (Ar.Game == EGame.GAME_Borderlands3)
            {
                SizeX = Ar.Read<ushort>();
                SizeY = Ar.Read<ushort>();
                SizeZ = Ar.Read<ushort>();
            }
            else
            {
                SizeX = Ar.Read<int>();
                SizeY = Ar.Read<int>();
                SizeZ = Ar.Game >= EGame.GAME_UE4_20 ? Ar.Read<int>() : 1;
            }

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.TEXTURE_DERIVED_DATA2 && !cooked)
            {
                var derivedDataKey = Ar.ReadFString();
            }
        }
    }

    public class FTexture2DMipMapConverter : JsonConverter<FTexture2DMipMap>
    {
        public override void WriteJson(JsonWriter writer, FTexture2DMipMap value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("BulkData");
            serializer.Serialize(writer, value.BulkData);

            writer.WritePropertyName("SizeX");
            writer.WriteValue(value.SizeX);

            writer.WritePropertyName("SizeY");
            writer.WriteValue(value.SizeY);

            writer.WritePropertyName("SizeZ");
            writer.WriteValue(value.SizeZ);

            writer.WriteEndObject();
        }

        public override FTexture2DMipMap ReadJson(JsonReader reader, Type objectType, FTexture2DMipMap existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

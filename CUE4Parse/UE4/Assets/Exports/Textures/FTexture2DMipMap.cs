using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    [JsonConverter(typeof(FTexture2DMipMapConverter))]
    public class FTexture2DMipMap
    {
        public readonly FByteBulkData Data;
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        public FTexture2DMipMap(FAssetArchive Ar)
        {
            SizeZ = 1;
            var cooked = Ar.Ver >= UE4Version.VER_UE4_TEXTURE_SOURCE_ART_REFACTOR && Ar.ReadBoolean();
            
            Data = new FByteBulkData(Ar);

            SizeX = Ar.Read<int>();
            SizeY = Ar.Read<int>();
            if (Ar.Game >= EGame.GAME_UE4_20)
            {
                SizeZ = Ar.Read<int>();    
            }

            if (Ar.Ver >= UE4Version.VER_UE4_TEXTURE_DERIVED_DATA2 && !cooked)
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
            serializer.Serialize(writer, value.Data);
            
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
using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public enum EVirtualTextureCodec : byte
    {
        Black,			//Special case codec, always outputs black pixels 0,0,0,0
        OpaqueBlack,	//Special case codec, always outputs opaque black pixels 0,0,0,255
        White,			//Special case codec, always outputs white pixels 255,255,255,255
        Flat,			//Special case codec, always outputs 128,125,255,255 (flat normal map)
        RawGPU,			//Uncompressed data in an GPU-ready format (e.g R8G8B8A8, BC7, ASTC, ...)
        ZippedGPU,		//Same as RawGPU but with the data zipped
        Crunch,			//Use the Crunch library to compress data
        Max,			// Add new codecs before this entry
    };

    [JsonConverter(typeof(FVirtualTextureDataChunkConverter))]
    public class FVirtualTextureDataChunk
    {
        public readonly FByteBulkData BulkData;
        public readonly uint SizeInBytes;
        public readonly uint CodecPayloadSize;
        public readonly uint[] CodecPayloadOffset;
        public readonly EVirtualTextureCodec[] CodecType;

        public FVirtualTextureDataChunk(FAssetArchive Ar, uint numLayers)
        {
            CodecType = new EVirtualTextureCodec[numLayers];
            CodecPayloadOffset = new uint[numLayers];
            if (Ar.Game >= EGame.GAME_UE5_0)
                Ar.Position += FSHAHash.SIZE; // var bulkDataHash = new FSHAHash(Ar);

            SizeInBytes = Ar.Read<uint>();
            CodecPayloadSize = Ar.Read<uint>();
            for (uint layerIndex = 0u; layerIndex < numLayers; ++layerIndex)
            {
                CodecType[layerIndex] = Ar.Read<EVirtualTextureCodec>();
                CodecPayloadOffset[layerIndex] = Ar.Game >= EGame.GAME_UE4_27 ? Ar.Read<uint>() : Ar.Read<ushort>();
            }
            BulkData = new FByteBulkData(Ar);
        }
    }

    public class FVirtualTextureDataChunkConverter : JsonConverter<FVirtualTextureDataChunk>
    {
        public override void WriteJson(JsonWriter writer, FVirtualTextureDataChunk value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("BulkData");
            serializer.Serialize(writer, value.BulkData);

            writer.WritePropertyName("SizeInBytes");
            writer.WriteValue(value.SizeInBytes);

            writer.WritePropertyName("CodecPayloadSize");
            writer.WriteValue(value.CodecPayloadSize);

            writer.WritePropertyName("CodecPayloadOffset");
            serializer.Serialize(writer, value.CodecPayloadOffset);

            writer.WritePropertyName("CodecType");
            writer.WriteStartArray();
            foreach (var codec in value.CodecType)
            {
                writer.WriteValue(codec.ToString());
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override FVirtualTextureDataChunk ReadJson(JsonReader reader, Type objectType, FVirtualTextureDataChunk existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

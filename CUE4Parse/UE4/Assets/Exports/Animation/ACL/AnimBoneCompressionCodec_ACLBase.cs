using System;
using CUE4Parse.ACL;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    [JsonConverter(typeof(FACLCompressedAnimDataConverter))]
    public class FACLCompressedAnimData : ICompressedAnimData
    {
        public int CompressedNumberOfFrames { get; set; }

        /** Holds the compressed_tracks instance */
        public byte[] CompressedByteStream;

        public CompressedTracks GetCompressedTracks() => new(CompressedByteStream);

        public void Bind(byte[] bulkData) => CompressedByteStream = bulkData;
    }

    public class FACLCompressedAnimDataConverter : JsonConverter<FACLCompressedAnimData>
    {
        public override void WriteJson(JsonWriter writer, FACLCompressedAnimData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("CompressedNumberOfFrames");
            writer.WriteValue(value.CompressedNumberOfFrames);

            /*writer.WritePropertyName("CompressedByteStream");
            writer.WriteValue(value.CompressedByteStream);*/

            writer.WriteEndObject();
        }

        public override FACLCompressedAnimData ReadJson(JsonReader reader, Type objectType, FACLCompressedAnimData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    /** The base codec implementation for ACL support. */
    public abstract class UAnimBoneCompressionCodec_ACLBase : UAnimBoneCompressionCodec
    {
        public override ICompressedAnimData AllocateAnimData() => new FACLCompressedAnimData();
    }
}
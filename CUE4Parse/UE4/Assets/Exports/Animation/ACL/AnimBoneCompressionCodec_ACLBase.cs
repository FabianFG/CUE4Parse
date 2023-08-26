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

    /** The base codec implementation for ACL support. */
    public abstract class UAnimBoneCompressionCodec_ACLBase : UAnimBoneCompressionCodec
    {
        public override ICompressedAnimData AllocateAnimData() => new FACLCompressedAnimData();
    }
}

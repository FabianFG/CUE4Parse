using CUE4Parse.ACL;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public class FACLCompressedAnimData : ICompressedAnimData
    {
        public int CompressedNumberOfFrames { get; set; }

        /** Holds the compressed_tracks instance */
        public byte[] CompressedByteStream;

        public CompressedTracks GetCompressedTracks() => new(CompressedByteStream);

        public void Bind(byte[] bulkData) => CompressedByteStream = bulkData;
    }

    /** The base codec implementation for ACL support. */
    public abstract class UAnimBoneCompressionCodec_ACLBase : UAnimBoneCompressionCodec { }
}
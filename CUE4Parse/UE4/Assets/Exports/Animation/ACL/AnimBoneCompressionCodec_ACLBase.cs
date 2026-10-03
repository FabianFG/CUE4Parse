using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL;

[JsonConverter(typeof(FACLCompressedAnimDataConverter))]
public class FACLCompressedAnimData : ICompressedAnimData
{
    public int CompressedNumberOfFrames { get; set; }

    /** Holds the compressed_tracks instance */
    public byte[] CompressedByteStream;

    public bool bCompressionFailed;

    public CompressedTracks GetCompressedTracks() => new(CompressedByteStream);

    public void Bind(byte[] bulkData) => CompressedByteStream = bulkData;

    public void SerializeCompressedData(FAssetArchive Ar)
    {
        ((ICompressedAnimData) this).BaseSerializeCompressedData(Ar);
        if (Ar.Game >= GAME_UE5_5)
        {
            bCompressionFailed = Ar.ReadBoolean();
        }
    }
}

/** The base codec implementation for ACL support. */
public abstract class UAnimBoneCompressionCodec_ACLBase : UAnimBoneCompressionCodec
{
    public override ICompressedAnimData AllocateAnimData() => new FACLCompressedAnimData();
}

using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL;

[JsonConverter(typeof(FACLDatabaseCompressedAnimDataConverter))]
public class FACLDatabaseCompressedAnimData : FACLCompressedAnimData
{
    /** The codec instance that owns us. */
    public UAnimBoneCompressionCodec_ACLDatabase? Codec;

    /** The sequence name hash that owns this data. */
    public uint SequenceNameHash;

    /*/** Holds the compressed_tracks instance for the anim sequence #1#
    public byte[] CompressedClip;*/

    public new void SerializeCompressedData(FAssetArchive Ar)
    {
        base.SerializeCompressedData(Ar);

        SequenceNameHash = Ar.Read<uint>();

        /*if (!Ar.Owner.HasFlags(EPackageFlags.PKG_FilterEditorOnly))
        {
            CompressedClip = Ar.ReadArray<byte>();
        }*/
    }

    public void Bind(byte[] bulkData)
    {
        //var compressedClipData = new CompressedTracks(bulkData);
        throw new NotImplementedException();
    }
}

public class UAnimBoneCompressionCodec_ACLDatabase : UAnimBoneCompressionCodec_ACLBase
{
    public override ICompressedAnimData AllocateAnimData() => new FACLDatabaseCompressedAnimData { Codec = this };
}

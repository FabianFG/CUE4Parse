using CUE4Parse.ACL;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public class UAnimBoneCompressionCodec_ACL : UAnimBoneCompressionCodec_ACLBase
    {
        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            var animData = (FACLCompressedAnimData) decompContext.CompressedAnimData;
            var compressedClipData = animData.GetCompressedTracks();
            //Trace.Assert(compressedClipData._handle != IntPtr.Zero && compressedClipData.IsValid(false) == null); access violation

            var aclContext = new DecompressionContext();
            aclContext.Initialize(compressedClipData);

            ACLDecompressionImpl.DecompressBone(decompContext, aclContext, trackIndex, out outAtom);
        }
    }
}
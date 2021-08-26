using System;
using System.Diagnostics;
using CUE4Parse.ACL;
using CUE4Parse.UE4.Assets.Exports.Animation.Codec;

namespace CUE4Parse.UE4.Assets.Exports.Animation.ACL
{
    public class UAnimBoneCompressionCodec_ACL : UAnimBoneCompressionCodec_ACLBase
    {
        public override void DecompressPose(FAnimSequenceDecompressionContext decompContext, BoneTrackPair[] rotationPairs, BoneTrackPair[] translationPairs, BoneTrackPair[] scalePairs, FTransform[] outAtoms)
        {
            var animData = (FACLCompressedAnimData) decompContext.CompressedAnimData;
            var compressedClipData = animData.GetCompressedTracks();
            Trace.Assert(compressedClipData._handle != IntPtr.Zero && compressedClipData.IsValid(false) == null);

            var aclContext = new DecompressionContext();
            aclContext.Initialize(compressedClipData);

            ACLDecompressionImpl.DecompressPose(decompContext, aclContext, rotationPairs, translationPairs, scalePairs, outAtoms);
        }

        public override void DecompressBone(FAnimSequenceDecompressionContext decompContext, int trackIndex, out FTransform outAtom)
        {
            var animData = (FACLCompressedAnimData) decompContext.CompressedAnimData;
            var compressedClipData = animData.GetCompressedTracks();
            Trace.Assert(compressedClipData._handle != IntPtr.Zero && compressedClipData.IsValid(false) == null);

            var aclContext = new DecompressionContext();
            aclContext.Initialize(compressedClipData);

            ACLDecompressionImpl.DecompressBone(decompContext, aclContext, trackIndex, out outAtom);
        }
    }
}
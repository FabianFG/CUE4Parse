using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    internal class AEFPerTrackCompressionCodec : AnimEncoding
    {
        public void GetBoneAtomRotation(
            FArchive Ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom)
        {
            /*var animData = (FUECompressedAnimData) decompContext.CompressedAnimData;
            var trackData = animData.CompressedTrackOffsets;
            var trackDataStartIndex = trackIndex * 2;
            var rotKeysOffset = trackData[trackDataStartIndex + 1];*/
            throw new NotImplementedException();
        }

        public void GetBoneAtomTranslation(
            FArchive Ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom)
        {
            throw new NotImplementedException();
        }

        public void GetBoneAtomScale(
            FArchive Ar,
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            ref FTransform outAtom)
        {
            throw new NotImplementedException();
        }
    }
}
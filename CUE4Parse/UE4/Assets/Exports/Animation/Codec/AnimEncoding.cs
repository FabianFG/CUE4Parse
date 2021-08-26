using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    public class AnimEncoding
    {
        public virtual void GetPoseRotations(FArchive ar, FTransform[] atoms, BoneTrackPair[] desiredPairs, FAnimSequenceDecompressionContext decompContext){}
        public virtual void GetPoseTranslations(FArchive ar, FTransform[] atoms, BoneTrackPair[] desiredPairs, FAnimSequenceDecompressionContext decompContext) { }
        public virtual void GetPoseScales(FArchive ar, FTransform[] atoms, BoneTrackPair[] desiredPairs, FAnimSequenceDecompressionContext decompContext) { }
    }
}
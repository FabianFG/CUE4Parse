using System;
using System.Collections.Generic;
using System.Text;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    abstract class AnimEncodingLegacyBase : IAnimEncoding
    {
        public abstract void GetBoneAtomRotation(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            out FTransform outAtom);

        public abstract void GetBoneAtomTranslation(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            out FTransform outAtom);

        public abstract void GetBoneAtomScale(
            FAnimSequenceDecompressionContext decompContext,
            int trackIndex,
            out FTransform outAtom);
    }
}
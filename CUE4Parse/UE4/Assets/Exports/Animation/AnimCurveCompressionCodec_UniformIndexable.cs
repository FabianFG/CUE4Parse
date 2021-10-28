using System;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimCurveCompressionCodec_UniformIndexable : UAnimCurveCompressionCodec
    {
        public override FFloatCurve[] ConvertCurves(UAnimSequence animSeq)
        {
            return Array.Empty<FFloatCurve>(); // TODO
        }
    }
}
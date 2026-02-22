using CUE4Parse.UE4.Objects.Engine.Animation;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCurveCompressionCodec_UniformlySampled : UAnimCurveCompressionCodec
{
    public override FFloatCurve[] ConvertCurves(FSmartName[] names, byte[] data)
    {
        return []; // TODO
    }
}

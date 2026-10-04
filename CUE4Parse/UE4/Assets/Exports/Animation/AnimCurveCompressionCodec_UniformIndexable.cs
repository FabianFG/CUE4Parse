using CUE4Parse.UE4.Objects.Engine.Animation;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimCurveCompressionCodec_UniformIndexable : UAnimCurveCompressionCodec
{
    public override FFloatCurve[] ConvertCurves(FSmartName[] names, byte[] data)
    {
        Log.Error($"{nameof(UAnimCurveCompressionCodec_UniformIndexable)} not supported");
        return []; // TODO
    }
}

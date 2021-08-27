namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    internal class AEFConstantKeyLerpShared : AnimEncodingLegacyBase
    {
        protected readonly AnimationCompressionFormat _format;

        protected AEFConstantKeyLerpShared(AnimationCompressionFormat format) => _format = format;
    }
}
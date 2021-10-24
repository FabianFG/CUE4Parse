namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    /// Base class for all curve compression codecs.
    public abstract class UAnimCurveCompressionCodec : UObject
    {
        public virtual UAnimCurveCompressionCodec? GetCodec(string path) => this;
    }
}
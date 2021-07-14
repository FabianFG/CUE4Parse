namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    /**
     * Indicates animation data compression format.
     */
    public enum AnimationCompressionFormat : byte
    {
        ACF_None,
        ACF_Float96NoW,
        ACF_Fixed48NoW,
        ACF_IntervalFixed32NoW,
        ACF_Fixed32NoW,
        ACF_Float32NoW,
        ACF_Identity,
        ACF_MAX
    }
}
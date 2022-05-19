namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public enum EAdditiveBasePoseType
    {
        /** Will be deprecated. */
        ABPT_None,
        /** Use the Skeleton's ref pose as base. */
        ABPT_RefPose,
        /** Use a whole animation as a base pose. BasePoseSeq must be set. */
        ABPT_AnimScaled,
        /** Use one frame of an animation as a base pose. BasePoseSeq and RefFrameIndex must be set (RefFrameIndex will be clamped). */
        ABPT_AnimFrame,
        /** Use one frame of this animation. RefFrameIndex must be set (RefFrameIndex will be clamped). */
        ABPT_LocalAnimFrame,
        ABPT_MAX,
    }
}

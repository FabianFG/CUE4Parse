using System;

namespace CUE4Parse.UE4.Wwise.Enums
{
    [Flags]
    public enum EBitsPositioning : byte
    {
        None = 0,
        PositioningInfoOverrideParent = 1 << 0,
        HasListenerRelativeRouting = 1 << 1,
        PannerTypeMask = 0b0001_1100,
        EmitterWithAutomation = 1 << 5, // low bit of e3DPositionType
        EmitterWithListenerRouting = 1 << 6,
    }

    public enum EPannerType : byte
    {
        DirectSpeakerAssignment = 0,
        BalanceFadeHeight = 1,
        SteeringPanner = 2,
        ThreeDSpatialization = 3,
    }

    public static class EBitsPositioningExt
    {
        public static bool IsEmitter(this EBitsPositioning b)
            => b.HasFlag(EBitsPositioning.EmitterWithAutomation)
            || b.HasFlag(EBitsPositioning.EmitterWithListenerRouting);

        public static EPannerType GetPannerType(this EBitsPositioning b)
            => (EPannerType) (((byte) b >> 2) & 0b111); // bits 2,3,4
    }
}

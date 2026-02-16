using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EBitsPositioning : byte
{
    PositioningInfoOverrideParent = 1 << 0,
    HasListenerRelativeRouting = 1 << 1,
    // v112+
    Unknown2d_2 = 1 << 2,
    Unknown2d_3 = 1 << 3,
    Unknown3d_4 = 1 << 4,
    Unknown3d_5 = 1 << 5,
    Unknown3d_6 = 1 << 6,
    Unknown3d_7 = 1 << 7,
    // v122+
    // Unknown2d_1 = 1 << 1, // overlaps HasListenerRelativeRouting
    Is3DPositioningAvailable_122 = 1 << 3,
    // v129+
    Is3DPositioningAvailable_129 = 1 << 4,
    // v130+
    PannerTypeMask = 0b0000_1100,
    PositionTypeMask = 0b0110_0000,
}

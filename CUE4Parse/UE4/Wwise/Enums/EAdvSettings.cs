using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum EAdvSettings : byte
{
    None = 0,
    KillNewest = 1 << 0,
    UseVirtualBehavior = 1 << 1,
    IgnoreParentMaxNumInst = 1 << 3,
    VVoicesOptOverrideParent = 1 << 4
}

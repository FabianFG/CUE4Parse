using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EAltValues : uint
{
    None = 0x0,
    UAlignment = 0x10,
    BDeviceAllocated = 0x10000
}

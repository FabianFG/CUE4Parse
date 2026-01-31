using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum ECurveScaling : byte
{
    None,
    // Unsupported = 0x1
    dB = 0x2,
    Log,
    dBToLin
}

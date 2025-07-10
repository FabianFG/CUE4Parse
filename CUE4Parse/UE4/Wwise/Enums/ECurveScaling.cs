using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum ECurveScaling : byte
{
    None,
    dB,
    Log,
    dBToLin
}

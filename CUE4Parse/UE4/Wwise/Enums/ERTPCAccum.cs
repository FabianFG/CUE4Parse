using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum ERTPCAccum : byte
{
    None,
    Exclusive,
    Additive,
    Multiply,
    Boolean,
    Maximum,
    Filter
}

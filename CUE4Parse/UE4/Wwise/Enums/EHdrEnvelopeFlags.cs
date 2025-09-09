using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum EHdrEnvelopeFlags : byte
{
    None = 0,
    OverrideHdrEnvelope = 1 << 0,
    OverrideAnalysis = 1 << 1,
    NormalizeLoudness = 1 << 2, 
    EnableEnvelope = 1 << 3
}

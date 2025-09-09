using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum EResumeOptions : byte
{
    None = 0,
    IsMasterResume = 1 << 0,
    ApplyToStateTransitions = 1 << 1,
    ApplyToDynamicSequence = 1 << 2
}

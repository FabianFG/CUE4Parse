using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum EMidiBehaviorFlags : byte
{
    None = 0,
    PriorityOverrideParent = 1 << 0,
    PriorityApplyDistFactor = 1 << 1,
    OverrideMidiEventsBehavior = 1 << 2,
    OverrideMidiNoteTracking = 1 << 3,
    EnableMidiNoteTracking = 1 << 4,
    IsMidiBreakLoopOnNoteOff = 1 << 5
}


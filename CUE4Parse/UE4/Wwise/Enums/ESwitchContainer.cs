using System;

namespace CUE4Parse.UE4.Wwise.Enums
{
    [Flags]
    public enum ESwitchContainer : byte
    {
        None = 0,
        PriorityOverrideParent = 1 << 0,            // 0x01
        PriorityApplyDistFactor = 1 << 1,           // 0x02
        OverrideMidiEventsBehavior = 1 << 2,        // 0x04
        OverrideMidiNoteTracking = 1 << 3,          // 0x08
        EnableMidiNoteTracking = 1 << 4,            // 0x10
        IsMidiBreakLoopOnNoteOff = 1 << 5,          // 0x20
    }
}

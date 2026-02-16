using System;

namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EMusicFlags : byte
{
    None = 0,
    OverrideParentMidiTempo = 1 << 1,
    OverrideParentMidiTarget = 1 << 2,
    MidiTargetTypeBus = 1 << 3 // (only present in v113–v152)
}

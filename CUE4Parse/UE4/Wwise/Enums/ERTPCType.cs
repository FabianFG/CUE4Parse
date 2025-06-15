using System;

namespace CUE4Parse.UE4.Wwise.Enums;

[Flags]
public enum ERTPCType : byte
{
    GameParameter,
    MidiParameter,
    Switch,
    State,
    Modulator
}

namespace CUE4Parse.UE4.Wwise.Enums;

public enum ESyncType : uint
{
    Immediate = 0x0,
    NextGrid = 0x1,
    NextBar = 0x2,
    NextBeat = 0x3,
    NextMarker = 0x4,
    NextUserMarker = 0x5,
    EntryMarker = 0x6,
    ExitMarker = 0x7,
    ExitNever = 0x8,
    LastExitPosition = 0x9
}


namespace CUE4Parse.UE4.Wwise.Enums.Flags;

[Flags]
public enum EPauseOptionsFlags : byte
{
    None = 0,
    PausePendingResume = 1 << 0,
    ApplyToStateTransitions = 1 << 1,
    ApplyToDynamicSequence = 1 << 2
}

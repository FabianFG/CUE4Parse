using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

// Doesn't actually exist in Wwise but it's reused, RANGED_PARAMETER<float>
public readonly struct AkRandomizerModifier
{
    public readonly float Base;
    public readonly float Min;
    public readonly float Max;

    public AkRandomizerModifier(FArchive Ar)
    {
        Base = Ar.Read<float>();
        Min = Ar.Read<float>();
        Max = Ar.Read<float>();
    }
}

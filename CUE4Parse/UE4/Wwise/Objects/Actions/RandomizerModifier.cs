using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class RandomizerModifier
{
    public readonly float Base;
    public readonly float Min;
    public readonly float Max;

    public RandomizerModifier(FArchive Ar)
    {
        Base = Ar.Read<float>();
        Min = Ar.Read<float>();
        Max = Ar.Read<float>();
    }
}

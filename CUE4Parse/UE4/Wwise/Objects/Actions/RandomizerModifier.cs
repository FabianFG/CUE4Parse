using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class RandomizerModifier
{
    public float Base { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }

    public RandomizerModifier(FArchive Ar)
    {
        Base = Ar.Read<float>();
        Min = Ar.Read<float>();
        Max = Ar.Read<float>();
    }
}

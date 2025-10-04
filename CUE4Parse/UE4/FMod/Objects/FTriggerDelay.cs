using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FTriggerDelay
{
    public readonly float Min;
    public readonly float Max;

    public FTriggerDelay(BinaryReader Ar)
    {
        Min = Ar.ReadSingle();
        Max = Ar.ReadSingle();
    }
}

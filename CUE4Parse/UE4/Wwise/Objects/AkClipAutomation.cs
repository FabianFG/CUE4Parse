using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkClipAutomation
{
    public readonly uint UClipIndex;
    public readonly uint EAutoType;
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkClipAutomation(FArchive Ar)
    {
        UClipIndex = Ar.Read<uint>();
        EAutoType = Ar.Read<uint>();
        GraphPoints = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRtpcGraphPoint(Ar));
    }
}

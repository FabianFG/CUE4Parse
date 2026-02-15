using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStateTransition
{
    public readonly uint StateFrom;
    public readonly uint StateTo;
    public readonly uint TransitionTime;

    public AkStateTransition(FArchive Ar)
    {
        StateFrom = Ar.Read<uint>();
        StateTo = Ar.Read<uint>();
        TransitionTime = Ar.Read<uint>();
    }
};

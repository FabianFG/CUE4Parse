using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStateGroupInfo
{
    public readonly uint StateGroupId;
    public readonly uint DefaultTransitionTime;
    public readonly AkStateTransition[] StateTransitions;

    public AkStateGroupInfo(FArchive Ar)
    {
        StateGroupId = Ar.Read<uint>();
        DefaultTransitionTime = Ar.Read<uint>();
        if (WwiseVersions.Version <= 52)
        {
            // To-Do
        }
        StateTransitions = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkStateTransition(Ar));
    }
}

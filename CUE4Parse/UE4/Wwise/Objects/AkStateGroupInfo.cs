namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStateGroupInfo
{
    public readonly uint StateGroupId;
    public readonly uint DefaultTransitionTime;
    public readonly AkStateTransition[] StateTransitions;

    public AkStateGroupInfo(FWwiseArchive Ar)
    {
        StateGroupId = Ar.Read<uint>();
        DefaultTransitionTime = Ar.Read<uint>();
        StateTransitions = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkStateTransition(Ar));
    }
}

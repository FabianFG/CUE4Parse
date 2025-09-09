using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetState
{
    public readonly uint SwitchGroupId;
    public readonly uint TargetStateId;

    public AkActionSetState(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        TargetStateId = Ar.Read<uint>();
    }
}

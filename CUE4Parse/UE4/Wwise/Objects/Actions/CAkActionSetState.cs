using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetState
{
    public readonly uint StateGroupId;
    public readonly uint TargetStateId;

    public CAkActionSetState(FArchive Ar)
    {
        StateGroupId = Ar.Read<uint>();
        TargetStateId = Ar.Read<uint>();
    }
}

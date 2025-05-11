using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetState
{
    public uint SwitchGroupId { get; protected set; }
    public uint TargetStateId { get; protected set; }

    public AkActionSetState(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        TargetStateId = Ar.Read<uint>();
    }
}

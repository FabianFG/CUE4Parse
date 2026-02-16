using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetSwitch
{
    public readonly uint SwitchGroupId;
    public readonly uint SwitchStateId;

    // CAkActionSetSwitch::SetActionParams
    public CAkActionSetSwitch(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        SwitchStateId = Ar.Read<uint>();
    }
}

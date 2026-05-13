namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public struct CAkActionSetSwitch
{
    public readonly uint SwitchGroupId;
    public readonly uint SwitchStateId;

    // CAkActionSetSwitch::SetActionParams
    public CAkActionSetSwitch(FWwiseArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        SwitchStateId = Ar.Read<uint>();
    }
}

using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetSwitch
{
    public readonly uint SwitchGroupId;
    public readonly uint SwitchStateId;

    public AkActionSetSwitch(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        SwitchStateId = Ar.Read<uint>();
    }
}

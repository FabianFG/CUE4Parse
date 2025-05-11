using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionSetSwitch
{
    public uint SwitchGroupId { get; protected set; }
    public uint SwitchStateId { get; protected set; }

    public AkActionSetSwitch(FArchive Ar)
    {
        SwitchGroupId = Ar.Read<uint>();
        SwitchStateId = Ar.Read<uint>();
    }
}

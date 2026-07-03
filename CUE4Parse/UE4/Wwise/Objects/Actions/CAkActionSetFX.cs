namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetFX
{
    public readonly bool IsAudioDeviceElement;
    public readonly byte SlotIndex;
    public readonly uint FxId;
    public readonly bool IsShared;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionSetFX::SetActionParams
    public CAkActionSetFX(FWwiseArchive Ar)
    {
        IsAudioDeviceElement = Ar.ReadBool();
        SlotIndex = Ar.Read<byte>();
        FxId = Ar.Read<uint>();
        IsShared = Ar.ReadBool();
        ExceptParams = new CAkActionExcept(Ar);
    }
}

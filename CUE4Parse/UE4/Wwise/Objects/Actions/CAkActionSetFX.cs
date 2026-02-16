using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionSetFX
{
    public readonly bool IsAudioDeviceElement;
    public readonly byte SlotIndex;
    public readonly uint FxId;
    public readonly bool IsShared;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionSetFX::SetActionParams
    public CAkActionSetFX(FArchive Ar)
    {
        IsAudioDeviceElement = Ar.Read<byte>() != 0;
        SlotIndex = Ar.Read<byte>();
        FxId = Ar.Read<uint>();
        IsShared = Ar.Read<byte>() != 0;
        ExceptParams = new CAkActionExcept(Ar);
    }
}

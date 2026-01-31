using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionBypassFX
{
    public readonly bool bIsBypass;
    public readonly byte TargetMask;
    public readonly byte byFxSlot;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionBypassFX::SetActionParams
    public CAkActionBypassFX(FArchive Ar)
    {
        bIsBypass = Ar.Read<byte>() != 0;

        switch (WwiseVersions.Version)
        {
            case >= 146:
                byFxSlot = Ar.Read<byte>();
                break;
            case >= 27:
                TargetMask = Ar.Read<byte>();
                break;
            default:
                break;
        }

        ExceptParams = new CAkActionExcept(Ar);
    }
}

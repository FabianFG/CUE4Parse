using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionBypassFX
{
    public bool bIsBypass;
    public byte TargetMask;
    public byte byFxSlot;
    public ExceptParams ExceptParams;

    public AkActionBypassFX(FArchive Ar)
    {
        bIsBypass = Ar.Read<byte>() != 0;

        if (WwiseVersions.Version >= 146)
        {
            byFxSlot = Ar.Read<byte>();
        }
        else if (WwiseVersions.Version >= 27)
        {
            TargetMask = Ar.Read<byte>();
        }

        ExceptParams = new ExceptParams(Ar);
    }
}

using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class CAkActionStop
{
    public readonly CAkActionParams ActionParams;
    public readonly bool ApplyToStateTransitions;
    public readonly bool ApplyToDynamicSequence;
    public readonly CAkActionExcept ExceptParams;

    // CAkActionStop::SetActionActiveParams
    public CAkActionStop(FArchive Ar)
    {
        ActionParams = new CAkActionParams(Ar);
        if (WwiseVersions.Version > 122)
        {
            var byBitVector = Ar.Read<byte>();
            ApplyToStateTransitions = (byBitVector & (1 << 1)) != 0; // bit 1
            ApplyToDynamicSequence = (byBitVector & (1 << 2)) != 0; // bit 2
        }

        ExceptParams = new CAkActionExcept(Ar);
    }
}

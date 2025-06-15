using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionStop
{
    public readonly ActionParams ActionParams;
    public readonly bool ApplyToStateTransitions;
    public readonly bool ApplyToDynamicSequence;
    public readonly ExceptParams ExceptParams;

    public AkActionStop(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);
        var byBitVector = Ar.Read<byte>();
        ApplyToStateTransitions = (byBitVector & (1 << 1)) != 0; // bit 1
        ApplyToDynamicSequence = (byBitVector & (1 << 2)) != 0; // bit 2
        ExceptParams = new ExceptParams(Ar);
    }
}

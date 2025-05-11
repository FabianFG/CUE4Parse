using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.Actions;

public class AkActionStop
{
    public ActionParams ActionParams { get; private set; }
    public bool ApplyToStateTransitions { get; private set; }
    public bool ApplyToDynamicSequence { get; private set; }
    public ExceptParams ExceptParams { get; private set; }

    public AkActionStop(FArchive Ar)
    {
        ActionParams = new ActionParams(Ar);
        var byBitVector = Ar.Read<byte>();
        ApplyToStateTransitions = (byBitVector & (1 << 1)) != 0; // bit 1
        ApplyToDynamicSequence = (byBitVector & (1 << 2)) != 0; // bit 2
        ExceptParams = new ExceptParams(Ar);
    }
}
